using Taskify.Models;

namespace Taskify.Services
{
    public interface IBoardService
    {
        // Lấy danh sách Board của user (cho trang chủ)
        Task<List<BoardViewModel>> GetBoardsByUserIdAsync(Guid userId);

        // Lấy chi tiết 1 Board (cho màn hình Kanban kéo thả)
        Task<BoardViewModel> GetBoardDetailsAsync(Guid boardId,Guid userId);

        // Tạo Board mới
        Task CreateBoardAsync(BoardCreateViewModel model, Guid userId);
        Task UpdateBoardAsync(BoardEditViewModel model, Guid userId);
        Task DeleteBoardAsync(Guid boardId, Guid userId);
        Task<BoardEditViewModel> GetBoardForEditAsync(Guid id);
        //===============LIST===================
        Task CreateListAsync(Guid boardId, string title, Guid userId);
        Task UpdateListOrderAsync(Guid boardId, Guid listId, int newIndex);
        Task DeleteListAsync(Guid listId, Guid userId);
        Task<TaskList> GetListByIdAsync(Guid listId);
        //
    }
}
