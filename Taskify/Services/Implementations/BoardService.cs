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
            // Lấy Board cá nhân HOẶC Board nhóm mà user tham gia
            return await _context.Boards
                .Where(b => b.OwnerId == userId || (b.Team != null && b.Team.Members.Any(m => m.UserId == userId)))
                .Select(b => new BoardViewModel
                {
                    Id = b.Id,
                    Name = b.Name,
                    TeamId = b.TeamId ?? Guid.Empty
                })
                .ToListAsync();
        }

        public async Task<BoardViewModel> GetBoardDetailsAsync(Guid boardId)
        {
            // Query lồng nhau: Board -> Lists -> Tasks -> Assignments -> User
            var board = await _context.Boards
                .Include(b => b.Lists)
                    .ThenInclude(l => l.Tasks)
                        .ThenInclude(t => t.Assignments)
                            .ThenInclude(a => a.User)
                .FirstOrDefaultAsync(b => b.Id == boardId);

            if (board == null) return null;

            // Map dữ liệu sang ViewModel
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
                            // Tạo chữ cái đầu (VD: "Nguyen Van" -> "NV")
                            Initials = string.Join("", a.User.FullName.Split(' ').Select(x => x[0])).ToUpper()
                        }).ToList()
                    }).ToList()
                }).ToList()
            };
        }

        public async Task CreateBoardAsync(string name, Guid userId)
        {
            var board = new Board
            {
                Id = Guid.NewGuid(),
                Name = name,
                OwnerId = userId,
                TeamId = null // Mặc định tạo board cá nhân
            };

            // Tạo sẵn 3 cột mặc định
            board.Lists = new List<TaskList>
            {
                new TaskList { Id = Guid.NewGuid(), Title = "To Do", Order = 0 },
                new TaskList { Id = Guid.NewGuid(), Title = "Doing", Order = 1 },
                new TaskList { Id = Guid.NewGuid(), Title = "Done", Order = 2 }
            };

            _context.Boards.Add(board);
            await _context.SaveChangesAsync();
        }
    }
}