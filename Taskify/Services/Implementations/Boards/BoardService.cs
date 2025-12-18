using Microsoft.EntityFrameworkCore;
using Taskify.Data;
using Taskify.Models;

namespace Taskify.Services
{
    public class BoardService : IBoardService
    {
        private readonly AppDbContext _context;

        public BoardService(AppDbContext context)
        {
            _context = context;
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
                    Desciption=b.Desciption,
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
                return new BoardViewModel
                {
                    Id = board.Id,
                    Name = board.Name,
                    TeamId = board.TeamId ?? Guid.Empty,
                    OwnerId=board.OwnerId,
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
            // Load Board kèm theo TẤT CẢ các bảng con cháu chắt
            // Để EF Core biết đường mà xóa (In-memory Cascade Delete)
            var board = await _context.Boards
                .Include(b => b.Team).ThenInclude(t => t.Members)
                .Include(b => b.Lists).ThenInclude(l => l.Tasks).ThenInclude(t => t.Assignments) // Load Assignments
                .Include(b => b.Lists).ThenInclude(l => l.Tasks).ThenInclude(t => t.Comments)    // Load Comments
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