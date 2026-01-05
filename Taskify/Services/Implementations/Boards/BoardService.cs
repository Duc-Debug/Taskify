using Microsoft.EntityFrameworkCore;
using Taskify.Data;
using Taskify.Models;

namespace Taskify.Services
{
    public class BoardService : IBoardService
    {
        private readonly AppDbContext _context;
        private readonly IActivityLogService _activityLogService;

        public BoardService(AppDbContext context, IActivityLogService activityLogService)
        {
            _context = context;
            _activityLogService = activityLogService;
        }

        public async Task<List<BoardViewModel>> GetBoardsByUserIdAsync(Guid userId)
        {
            return await _context.Boards
                .Where(b => b.OwnerId == userId || (b.Team != null && b.Team.Members.Any(m => m.UserId == userId)))
                .Select(b => new BoardViewModel
                {
                    Id = b.Id,
                    Name = b.Name,
                    TeamId = b.TeamId ?? Guid.Empty,
                    TeamName = b.Team != null ? b.Team.Name : null,
                    Desciption = b.Desciption,
                    TeamMembers = b.Team != null
                        ? b.Team.Members.Select(m => new MemberViewModel
                        {
                            Id = m.UserId,
                            FullName = m.User.FullName,
                            AvatarUrl = m.User.FullName.Substring(0, 1)
                        }).ToList()
                        : new List<MemberViewModel>(),
                    //CanCreateList chi cho Owner va Admin Team moi duoc phep tao List
                    CanCreateList = b.OwnerId == userId || (b.Team != null && b.Team.Members.Any(m => m.UserId == userId && m.Role == TeamRole.Admin)),
                    // Đếm số lượng List và Task để hiển thị ra ngoài Dashboard (nếu cần)
                    Lists = b.Lists.Select(l => new TaskListViewModel
                    {
                        Tasks = l.Tasks.Select(t => new TaskCardViewModel()).ToList()
                    }).ToList()
                })
                .ToListAsync();
        }

        public async Task<BoardViewModel> GetBoardDetailsAsync(Guid boardId, Guid userId)
        {
            var board = await _context.Boards
                .Include(b => b.Team).ThenInclude(t => t.Members).ThenInclude(m => m.User)
                .Include(b => b.Lists)
                    .ThenInclude(l => l.Tasks)
                        .ThenInclude(t => t.Assignments)
                            .ThenInclude(a => a.User)
                .Include(b => b.Team)
                    .ThenInclude(t => t.Members)
                .FirstOrDefaultAsync(b => b.Id == boardId);

            if (board == null) return null;

            var canCreate = false;
            if (board.Team != null)
            {
                var member = board.Team.Members.FirstOrDefault(m => m.UserId == userId);
                if (member != null)
                {
                    if (member.Role == TeamRole.Owner || member.Role == TeamRole.Admin) canCreate = true;
                }
            }
            else
            {
                if (board.OwnerId == userId)
                {
                    canCreate = true;
                }
            }
            var activities = await _activityLogService.GetBoardActivitiesAsync(boardId, 50);
            return new BoardViewModel
            {
                Id = board.Id,
                Name = board.Name,
                TeamId = board.TeamId ?? Guid.Empty,
                OwnerId = board.OwnerId,
                TeamMembers = board.Team != null
                     ? board.Team.Members.Select(m => new MemberViewModel
                     {
                         Id = m.UserId,
                         FullName = m.User.FullName,
                         AvatarUrl = m.User.AvatarUrl,
                         Initials = !string.IsNullOrEmpty(m.User.FullName)
                                           ? m.User.FullName.Substring(0, 1)
                                           : "U"
                     }).ToList()
                     : new List<MemberViewModel>(),
                CanCreateList = canCreate,
                Activities=activities,
                Lists = board.Lists.OrderBy(l => l.Order).Select(l => new TaskListViewModel
                {
                    Id = l.Id,
                    Title = l.Title,
                    Order = l.Order,
                    Tasks = l.Tasks.OrderBy(t => t.Order).Select(t => new TaskCardViewModel
                    {
                        Id = t.Id,
                        Title = t.Title,
                        Priority = t.Priority,
                        DueDate = t.DueDate,
                        Status = t.Status,
                        Assignees = t.Assignments.Select(a => new MemberViewModel
                        {
                            Id = a.User.Id,
                            FullName = a.User.FullName,
                            AvatarUrl = a.User.AvatarUrl,
                            Initials = !string.IsNullOrEmpty(a.User.FullName) ?
                                       string.Join("", a.User.FullName.Split(' ').Select(x => x[0])).ToUpper() : "U"
                        }).ToList()
                    }).ToList()
                }).ToList()
            };
        }

        public async Task CreateBoardAsync(BoardCreateViewModel model, Guid userId)
        {
            var board = new Board
            {
                Id = Guid.NewGuid(),
                Name = model.Name,
                OwnerId = userId,
                TeamId = model.BoardType == "personal" ? null : model.TeamId,
                Desciption = model.Description,
                CreatedAt = DateTime.Now
            };

            // Xử lý Template
            board.Lists = new List<TaskList>();
            switch (model.Template?.ToLower())
            {
                case "scrum":
                    board.Lists.Add(new TaskList { Id = Guid.NewGuid(), Title = "Backlog", Order = 0 });
                    board.Lists.Add(new TaskList { Id = Guid.NewGuid(), Title = "To Do", Order = 1 });
                    board.Lists.Add(new TaskList { Id = Guid.NewGuid(), Title = "In Progress", Order = 2 });
                    board.Lists.Add(new TaskList { Id = Guid.NewGuid(), Title = "Review", Order = 3 });
                    board.Lists.Add(new TaskList { Id = Guid.NewGuid(), Title = "Done", Order = 4 });
                    break;

                case "blank":
                    board.Lists.Add(new TaskList { Id = Guid.NewGuid(), Title = "Backlog", Order = 0 });
                    break;

                case "kanban":
                default:
                    board.Lists.Add(new TaskList { Id = Guid.NewGuid(), Title = "To Do", Order = 0 });
                    board.Lists.Add(new TaskList { Id = Guid.NewGuid(), Title = "Doing", Order = 1 });
                    board.Lists.Add(new TaskList { Id = Guid.NewGuid(), Title = "Done", Order = 2 });
                    break;
            }

            _context.Boards.Add(board);
            if (model.TeamId.HasValue)
            {
                await _activityLogService.LogAsync(userId, ActivityType.BoardCreated,
                    $"Created new board: {model.Name}", teamId: model.TeamId.Value, boardId: board.Id);
            }
            else
            {
                await _activityLogService.LogAsync(userId, ActivityType.BoardCreated,
                    $"Created personal new board: {model.Name}", teamId: null, boardId: board.Id);
            }
            await _context.SaveChangesAsync();
        }

        public async Task UpdateBoardAsync(BoardEditViewModel model, Guid userId)
        {
            var board = await _context.Boards.FirstOrDefaultAsync(b => b.Id == model.Id);
            if (board != null && (board.OwnerId == userId))
            {
                board.Name = model.Name;
                board.Desciption = model.Description;
                await _context.SaveChangesAsync();
            }
        }

        public async Task DeleteBoardAsync(Guid boardId, Guid userId)
        {
            var board = await _context.Boards
                .Include(b => b.Team).ThenInclude(t => t.Members)
                .Include(b => b.Lists).ThenInclude(l => l.Tasks).ThenInclude(t => t.Assignments) 
                .Include(b => b.Lists).ThenInclude(l => l.Tasks).ThenInclude(t => t.Comments)   
                .Include(b => b.Lists).ThenInclude(l => l.Tasks).ThenInclude(t => t.TaskHistories)
                .FirstOrDefaultAsync(b => b.Id == boardId);

            if (board == null) throw new Exception("Board is not exists");
            bool isCreator = board.OwnerId == userId;
            bool isTeamOwner = false;
            if (board.Team != null)
            {
                var member = board.Team.Members.FirstOrDefault(m => m.UserId == userId);
                if (member != null && member.Role == TeamRole.Owner) isTeamOwner = true;
            }
            if (isCreator || isTeamOwner)
            {
                if (board.TeamId.HasValue)
                {
                    await _activityLogService.LogAsync(userId, ActivityType.BoardDeleted,
                        $"Delete board: {board.Name}", teamId: board.TeamId.Value, boardId: null);
                }
                else
                {
                    await _activityLogService.LogAsync(userId, ActivityType.BoardDeleted,
                        $"Delete personal new board: {board.Name}", teamId: null, boardId: null);
                }
                _context.Boards.Remove(board);
                await _context.SaveChangesAsync();
            }
            else throw new Exception("You are not permission delete this Board. Only Creator or Team Owner can delete");

        }
        public async Task<BoardEditViewModel> GetBoardForEditAsync(Guid id)
        {
            var board = await _context.Boards.FindAsync(id);
            if (board == null) return null;

            return new BoardEditViewModel
            {
                Id = board.Id,
                Name = board.Name,
                Description = board.Desciption
            };
        }
        public async Task<Guid> CreateBoardFromAiAsync(AiBoardPlan plan, Guid userId, Guid? teamId)
        {
            HashSet<Guid> validUserIds;

            if (teamId.HasValue)
            {
                // Nếu là Team: Lấy tất cả ID thành viên trong team
                var teamMemberIds = await _context.TeamMembers
                    .Where(tm => tm.TeamId == teamId.Value)
                    .Select(tm => tm.UserId)
                    .ToListAsync();
                validUserIds = new HashSet<Guid>(teamMemberIds);
            }
            else
            {
                // Nếu là Personal: Chỉ có ID của chính mình là hợp lệ
                validUserIds = new HashSet<Guid> { userId };
            }

            // 2. Tạo đối tượng Board
            var newBoard = new Board
            {
                Id = Guid.NewGuid(),
                Name = plan.BoardName,
                Desciption = plan.Description,
                OwnerId = userId,
                TeamId = teamId,
                CreatedAt = DateTime.Now,
                Lists = new List<TaskList>()
            };

            if (plan.Lists != null)
            {
                int listOrder = 0;
                foreach (var aiList in plan.Lists)
                {
                    var newList = new TaskList
                    {
                        Id = Guid.NewGuid(),
                        Title = aiList.Title,
                        BoardId = newBoard.Id,
                        Order = listOrder++,
                        Tasks = new List<TaskItem>()
                    };

                    if (aiList.Tasks != null)
                    {
                        int taskOrder = 0;
                        foreach (var aiTask in aiList.Tasks)
                        {
                            Enum.TryParse(aiTask.Priority, true, out TaskPriority priority);

                            var newTask = new TaskItem
                            {
                                Id = Guid.NewGuid(),
                                Title = aiTask.Title,
                                Description = $"{aiTask.Description}\n\n>  **AI Suggestion:** {aiTask.ReasonForAssignment}",
                                Priority = priority,
                                Status = Models.TaskStatus.Pending,
                               
                                DueDate = DateTime.Now.AddDays(aiTask.DueInDays > 0 ? aiTask.DueInDays : 3),
                                CreatorId = userId,
                                Order = taskOrder++,
                                Assignments = new List<TaskAssignment>()
                            };

                            // 3. [MỚI] Validate Assignment
                            if (aiTask.AssignedUserId.HasValue)
                            {
                                // CHỈ GÁN NẾU ID TỒN TẠI TRONG LIST HỢP LỆ
                                if (validUserIds.Contains(aiTask.AssignedUserId.Value))
                                {
                                    newTask.Assignments.Add(new TaskAssignment
                                    {
                                        TaskId = newTask.Id,
                                        UserId = aiTask.AssignedUserId.Value,
                                        //AssignedAt = DateTime.Now
                                    });
                                }
                                else
                                {
                                    newTask.Description += $"\n\n*(Cảnh báo: AI đã cố gán cho User ID không tồn tại: {aiTask.AssignedUserId})*";
                                }
                            }

                            newList.Tasks.Add(newTask);
                        }
                    }
                    newBoard.Lists.Add(newList);
                }
            }

            _context.Boards.Add(newBoard);
            await _context.SaveChangesAsync();

            // 5. Ghi Log (An toàn vì Board đã lưu)
            try
            {
                string logMsg = teamId.HasValue ? $"AI created team board: {plan.BoardName}" : $"AI created personal board: {plan.BoardName}";
                await _activityLogService.LogAsync(userId, ActivityType.BoardCreated, logMsg, teamId: teamId, boardId: newBoard.Id);
            }
            catch
            {
                // Ignore log error
            }

            return newBoard.Id;
        }
        //============================
        //          LIST
        //=================================
        public async Task CreateListAsync(Guid boardId, string title, Guid userId)
        {
            var board = await _context.Boards
                  .Include(b => b.Lists)
                  .FirstOrDefaultAsync(b => b.Id == boardId);
            if (board != null && (board.OwnerId == userId || board.TeamId != null))
            {
                var newOrder = board.Lists.Any() ? board.Lists.Max(l => l.Order) + 1 : 0;

                // 3. Tạo List mới
                var newList = new TaskList
                {
                    Id = Guid.NewGuid(),
                    BoardId = boardId,
                    Title = title,
                    Order = newOrder,
                    Tasks = new List<TaskItem>() // Khởi tạo list rỗng
                };
                _context.TaskLists.Add(newList);
                await _context.SaveChangesAsync();
            }
        }

        public async Task UpdateListOrderAsync(Guid boardId, Guid listId, int newIndex)
        {
            var list = await _context.TaskLists
                 .Where(l => l.BoardId == boardId)
                 .OrderBy(l => l.Order)
                 .ToListAsync();

            var listToMove = list.FirstOrDefault(l => l.Id == listId);
            if (listToMove == null) return;
            list.Remove(listToMove);

            if (newIndex < 0) newIndex = 0;
            if (newIndex > list.Count) newIndex = list.Count;
            list.Insert(newIndex, listToMove);

            for (int i = 0; i < list.Count; i++) list[i].Order = i;
            _context.TaskLists.UpdateRange(list);
            await _context.SaveChangesAsync();
        }
        public async Task DeleteListAsync(Guid listId, Guid userId)
        {
            var list = await _context.TaskLists
                .Include(l => l.Board)
                .Include(l => l.Tasks)
                    .ThenInclude(t => t.Assignments) // Load Assignments
                .FirstOrDefaultAsync(l => l.Id == listId);
            if (list == null) throw new Exception("List not found");

            if (list.Tasks != null && list.Tasks.Any())
            {
                foreach (var task in list.Tasks)
                {
                    if (task.Assignments != null && task.Assignments.Any())
                    {
                        _context.TaskAssignments.RemoveRange(task.Assignments); // Xóa phân công
                    }
                    // _context.TaskComments.RemoveRange(task.Comments);
                }

                _context.Tasks.RemoveRange(list.Tasks);
            }
            _context.TaskLists.Remove(list);
            await _context.SaveChangesAsync();

        }
        public async Task<TaskList> GetListByIdAsync(Guid listId)
        {
            return await _context.TaskLists
                .Include(l => l.Board)
                .FirstOrDefaultAsync(l => l.Id == listId);
        }
       
    }
}