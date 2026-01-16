using Microsoft.EntityFrameworkCore;
using Taskify.Data;
using Taskify.Models;

namespace Taskify.Services.Implementations
{
    public class SkillEvaluationService : ISkillEvaluationService
    {
        private readonly AppDbContext _context;
        private readonly INotificationService _notificationService;

        public SkillEvaluationService(AppDbContext context, INotificationService notificationService)
        {
            _context = context;
            _notificationService = notificationService;
        }

        public async Task CheckAndSuggestSkillUpdateAsync(Guid taskId, Guid userId)
        {
            var task = await _context.Tasks.Include(t => t.List).FirstOrDefaultAsync(t => t.Id == taskId);
            var user = await _context.Users.Include(u => u.Skills).FirstOrDefaultAsync(u => u.Id == userId);

            if (task == null || user == null) return;

            //Dựa vào Title xác định skill(tạm)
            var matchedSkill = user.Skills.FirstOrDefault(s =>
                (task.Title + " " + task.Description).ToLower().Contains(s.SkillName.ToLower()));

            if (matchedSkill == null) return; 

            double taskDifficulty = task.Priority switch
            {
                TaskPriority.High => 9.0,   
                TaskPriority.Medium => 6.0, 
                TaskPriority.Low => 3.0,   
                _ => 5.0
            };

            double timeMultiplier = 1.0;
            if (task.CompletedAt.HasValue && task.DueDate.HasValue)
            {
                if (task.CompletedAt.Value > task.DueDate.Value)
                    timeMultiplier = 0.6;
                else if ((task.DueDate.Value - task.CompletedAt.Value).TotalDays > 1)
                    timeMultiplier = 1.2; 
            }

            double performanceScore = Math.Clamp(taskDifficulty * timeMultiplier, 1, 10);

            //Công thức cập nhật
            double currentSkill = matchedSkill.ProficiencyLevel;
            double learningRate = 0.15; 

            // Tính điểm mới
            double newSkillRaw = currentSkill + learningRate * (performanceScore - currentSkill);
            int newSkillRounded = (int)Math.Round(newSkillRaw);

            int diff = newSkillRounded - matchedSkill.ProficiencyLevel;

            if (Math.Abs(diff) >= 1) 
            {
                string message = "";
                if (diff > 0)
                    message = $"Excellent performance! You completed the task brilliantly.'{task.Title}'. Skill upgrade suggestion system{matchedSkill.SkillName} to {newSkillRounded}.";
                else
                    message = $"Warning: You have missed the deadline for task '{task.Title}'. The system suggests lowering skill {matchedSkill.SkillName} to {newSkillRounded} to reflect the actual situation.";

                await _notificationService.CreateInfoNotificationAsync(userId, message);
                await CreateSuggestionNotification(userId, matchedSkill.SkillName, newSkillRounded, message);
            }
        }
        public async Task<Dictionary<string, int>> EvaluateAllSkillsAsync(Guid userId)
        {
            var results = new Dictionary<string, int>();
            var user = await _context.Users
                .Include(u => u.Skills)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null || user.Skills == null || !user.Skills.Any())
                return results;

            
            var completedTasks = await _context.TaskAssignments
                .Include(ta => ta.Task)
                .Where(ta => ta.UserId == userId && ta.Task.Status == Models.TaskStatus.Completed)
                .OrderBy(ta => ta.Task.CompletedAt)
                .Select(ta => ta.Task)
                .ToListAsync();

            // 3. Tính toán cho từng Skill
            foreach (var skill in user.Skills)
            {
                double currentRating = skill.ProficiencyLevel;
                var relevantTasks = completedTasks.Where(t =>
                    (t.Title + " " + t.Description).ToLower().Contains(skill.SkillName.ToLower()));

                foreach (var task in relevantTasks)
                {
                    double difficultyScore = task.Priority switch
                    {
                        TaskPriority.High => 9.0,  
                        TaskPriority.Medium => 6.0, 
                        TaskPriority.Low => 3.0,    
                        _ => 5.0
                    };

                    double timeMultiplier = 1.0;
                    if (task.CompletedAt.HasValue && task.DueDate.HasValue)
                    {
                        if (task.CompletedAt.Value > task.DueDate.Value)
                            timeMultiplier = 0.6; 
                        else if ((task.DueDate.Value - task.CompletedAt.Value).TotalHours > 24)
                            timeMultiplier = 1.2; 
                    }

                    double performance = Math.Clamp(difficultyScore * timeMultiplier, 1, 10);

                    currentRating = currentRating + 0.15 * (performance - currentRating);
                }

                int finalRating = (int)Math.Round(Math.Clamp(currentRating, 1, 10));
                results.Add(skill.SkillName, finalRating);
            }

            return results;
        }
        private async Task CreateSuggestionNotification(Guid userId, string skillName, int newLevel, string message)
        {
            //Sẽ làm sau(còn bao h thì chưa biết),Se kieu notification nhan vao cap nhat
        }
    }
}
