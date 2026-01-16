using Microsoft.EntityFrameworkCore;
using Taskify.Data;

namespace Taskify.Services
{
    public class PerformanceService : IPerformanceService
    {
        private readonly AppDbContext _context;
        public PerformanceService(AppDbContext context) { _context = context; }

        public async Task<int> CalculateSuccessProbabilityAsync(Guid userId, string taskTitle, string taskDescription)
        {
            // 1. Điểm cơ bản
            int score = 50;

            var user = await _context.Users
                .Include(u => u.Skills)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null) return 0;

            // Lấy lịch sử task (đã hoàn thành hoặc đã quá hạn) để tính toán chính xác hơn
            var history = await _context.TaskAssignments
                .Include(t => t.Task)
                .Where(ta => ta.UserId == userId && ta.Task.List != null) // Safety check
                .ToListAsync();

            // 2. Tính điểm dựa trên lịch sử (40% trọng số)
            if (history.Any())
            {
                // Đếm số lượng
                int completedOnTime = history.Count(h => h.Task.Status == Models.TaskStatus.Completed
                                                      && h.Task.CompletedAt.HasValue
                                                      && h.Task.CompletedAt.Value <= h.Task.DueDate);

                int completeLate = history.Count(h => h.Task.Status == Models.TaskStatus.Completed
                                                   && h.Task.CompletedAt.HasValue
                                                   && h.Task.CompletedAt.Value > h.Task.DueDate);

                int currentlyOverDue = history.Count(h => h.Task.Status != Models.TaskStatus.Completed
                                                      && h.Task.DueDate.HasValue // Check null
                                                      && DateTime.Now > h.Task.DueDate.Value);

                int totalCompleted = completedOnTime + completeLate;

                // --- SỬA LỖI TÍNH TOÁN ---
                if (totalCompleted > 0)
                {
                    double onTimeRate = (double)completedOnTime / totalCompleted;
                    // Nhân trước rồi mới ép kiểu
                    score += (int)(onTimeRate * 30);
                }

                // Phạt nhẹ hơn chút để tránh điểm âm quá nhanh
                score -= (completeLate * 2);      // Giảm hình phạt từ 5 xuống 2
                score -= (currentlyOverDue * 5);  // Giảm hình phạt từ 10 xuống 5
            }
            else
            {
                // Người mới thì cho điểm trung bình khá để khích lệ (thay vì 40)
                score = 50;
            }

            // 3. Tính điểm kỹ năng (Cải thiện khớp từ khóa)
            // Kết hợp Title và Description để tìm kiếm
            string textToCheck = (taskTitle + " " + taskDescription).ToLower();

            // Flag để tránh cộng dồn quá nhiều nếu user có nhiều skill trùng lặp
            bool skillMatched = false;

            if (user.Skills != null)
            {
                foreach (var skill in user.Skills)
                {
                    // Kiểm tra null skill name
                    if (string.IsNullOrEmpty(skill.SkillName)) continue;

                    string skillName = skill.SkillName.ToLower();

                    // Logic khớp: Có thể mở rộng (ví dụ: C# khớp .NET, JS khớp JavaScript...)
                    if (textToCheck.Contains(skillName))
                    {
                        int bonus = skill.ProficiencyLevel switch
                        {
                            >= 9 => 20, // 9, 10
                            >= 6 => 15, // 6, 7, 8
                            >= 3 => 10, // 3, 4, 5
                            _ => 5      // 1, 2
                        };
                        score += bonus;
                        skillMatched = true;
                    }
                }
            }

            // Bonus nhỏ nếu có skill phù hợp (để đảm bảo điểm cao hơn người không có skill)
            if (skillMatched) score += 5;

            // 4. Kẹp giá trị từ 1 đến 99 (tăng max lên 100 nếu muốn)
            return Math.Clamp(score, 1, 99);
        }
    }
}
