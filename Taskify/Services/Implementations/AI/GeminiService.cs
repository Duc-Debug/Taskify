using Google.GenAI;
using System.Text.Json;
using Taskify.Models;
using Microsoft.Extensions.Configuration;

namespace Taskify.Services.Implementations.AI
{
    public class GeminiService:IGeminiService
    {
        private readonly string _apiKey;
        private readonly Client _client;

        public GeminiService(IConfiguration configuration)
        {
            _apiKey = configuration["Gemini:ApiKey"];

            // 2. Validate Key
            if (string.IsNullOrEmpty(_apiKey))
            {
                throw new ArgumentNullException(nameof(_apiKey), "API Key cho Gemini chưa được cấu hình trong AppSettings hoặc User Secrets.");
            }

            _client = new Client(apiKey: _apiKey);
        }

        public async Task<AiBoardPlan> GenerateBoardPlanAsync(string projectPrompt, List<User> teamMembers)
        {
            // Chuẩn bị dữ liệu nhân viên để đưa vào Prompt
            var membersData = teamMembers.Select(u => new
            {
                Id = u.Id,
                Name = u.FullName,
                JobTitle = u.JobTitle,
                Skills = u.Skills.Select(s => new { s.SkillName, s.ProficiencyLevel }).ToList()
            });

            string membersJson = JsonSerializer.Serialize(membersData);

            //Xây dựng Prompt 
            var prompt = $@"
                You are an expert Project Manager and AI Assistant using the Gemini 2.5 Flash model.
                
                **GOAL:** Create a project board structure (Lists and Tasks) based on the user's request.
                Crucially, assign each task to the MOST SUITABLE team member based on their skills and proficiency.

                **USER REQUEST:** ""{projectPrompt}""

                **AVAILABLE TEAM MEMBERS (JSON):**
                {membersJson}

                **INSTRUCTIONS:**
                1. Create logical Lists (e.g., To Do, In Progress, Done, or specific phases).
                2. Break down the project into actionable Tasks.
                3. Analyze the skills of the team members provided. 
                4. For each task, assign the `AssignedUserId` strictly from the provided members list.
                5. Provide a `ReasonForAssignment` explaining why that member fits (e.g., 'Matches C# skill level 9').
                6. Set Priority: Low, Medium, or High.

                **OUTPUT FORMAT:**
                You must return ONLY a valid JSON string matching the following C# class structure, no markdown formatting, no code blocks:

                {{
                    ""BoardName"": ""string"",
                    ""Description"": ""string"",
                    ""Lists"": [
                        {{
                            ""Title"": ""string"",
                            ""Tasks"": [
                                {{
                                    ""Title"": ""string"",
                                    ""Description"": ""string"",
                                    ""Priority"": ""string"",
                                    ""AssignedUserId"": ""Guid (must match one of the input IDs) or null"",
                                    ""ReasonForAssignment"": ""string""
                                }}
                            ]
                        }}
                    ]
                }}
            ";

            var response = await _client.Models.GenerateContentAsync(
                model: "gemini-2.5-flash",
                contents: prompt
            );

            // 4. Xử lý kết quả trả về
            string responseText = response?.Candidates?[0]?.Content?.Parts?[0]?.Text;

            if (string.IsNullOrEmpty(responseText))
            {
                return null;
            }

            responseText = responseText.Replace("```json", "").Replace("```", "").Trim();

            try
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                var plan = JsonSerializer.Deserialize<AiBoardPlan>(responseText, options);
                return plan;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error parsing AI response: {ex.Message}");
                Console.WriteLine($"Raw response: {responseText}");
                throw;
            }
        }
    }
}

