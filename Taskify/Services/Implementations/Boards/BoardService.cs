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
                    // Đếm số lượng List và Task để hiển thị ra ngoài Dashboard (nếu cần)
                    Lists = b.Lists.Select(l => new TaskListViewModel
                    {
                        Tasks = l.Tasks.Select(t => new TaskCardViewModel()).ToList()
                    }).ToList()
                })
                .ToListAsync();
        }

        public async Task<BoardViewModel> GetBoardDetailsAsync(Guid boardId)
        {
            var board = await _context.Boards
                .Include(b => b.Lists)
                    .ThenInclude(l => l.Tasks)
                        .ThenInclude(t => t.Assignments)
                            .ThenInclude(a => a.User)
                .FirstOrDefaultAsync(b => b.Id == boardId);

            if (board == null) return null;

            return new BoardViewModel
            {
                Id = board.Id,
                Name = board.Name,
                TeamId = board.TeamId ?? Guid.Empty,
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
                // [FIX LỖI 1] Dùng model.Name thay vì model.Title để khớp với View
                Name = model.Name,
                OwnerId = userId,
                TeamId = model.BoardType == "personal" ? null : model.TeamId,
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
            if (board != null && board.OwnerId == userId)
            {
                board.Name = model.Name;
                await _context.SaveChangesAsync();
            }
        }

        public async Task DeleteBoardAsync(Guid boardId, Guid userId)
        {
            // Load Board kèm theo TẤT CẢ các bảng con cháu chắt
            // Để EF Core biết đường mà xóa (In-memory Cascade Delete)
            var board = await _context.Boards
                .Include(b => b.Lists)
                    .ThenInclude(l => l.Tasks)
                        .ThenInclude(t => t.Assignments) // Load Assignments
                .Include(b => b.Lists)
                    .ThenInclude(l => l.Tasks)
                        .ThenInclude(t => t.Comments)    // Load Comments
                .Include(b => b.Lists)
                    .ThenInclude(l => l.Tasks)
                        .ThenInclude(t => t.TaskHistories) 
                .FirstOrDefaultAsync(b => b.Id == boardId);
           
            if (board != null && board.OwnerId == userId)
            {
                _context.Boards.Remove(board);
                await _context.SaveChangesAsync();
            }
        }
    }
}