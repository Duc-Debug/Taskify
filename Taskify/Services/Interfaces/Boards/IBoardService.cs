using Taskify.Models;

namespace Taskify.Services
{
    public interface IBoardService
    {
        // Lấy danh sách Board của user (cho trang chủ)
        Task<List<BoardViewModel>> GetBoardsByUserIdAsync(Guid userId);

        // Lấy chi tiết 1 Board (cho màn hình Kanban kéo thả)
        Task<BoardViewModel> GetBoardDetailsAsync(Guid boardId);

        // Tạo Board mới
        Task CreateBoardAsync(BoardCreateViewModel model, Guid userId);
        Task UpdateBoardAsync(BoardEditViewModel model, Guid userId);
        Task DeleteBoardAsync(Guid boardId, Guid userId);
    }
}
