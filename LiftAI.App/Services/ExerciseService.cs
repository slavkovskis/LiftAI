using System.Net.Http.Json;
using LiftAI.Shared.Models.Dtos.Exercise;

namespace LiftAI.App.Services;

public enum ExerciseWriteStatus
{
    Success,
    Unauthorized,
    Failed
}

public sealed class ExerciseUpdateResult
{
    public ExerciseWriteStatus Status { get; init; }
    public ExerciseDto? Exercise { get; init; }
}

public class ExerciseService(IHttpClientFactory httpClientFactory, ILogger<ExerciseService> logger)
{
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient("Api");

    public async Task<List<ExerciseDto>> GetAllExercisesAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("api/exercises");

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<List<ExerciseDto>>()
                       ?? [];
            }

            if (response.StatusCode is System.Net.HttpStatusCode.Unauthorized or System.Net.HttpStatusCode.Forbidden)
            {
                return await GetTrialExercisesAsync();
            }

            logger.LogError("Failed to fetch exercises. Status code: {StatusCode}", response.StatusCode);
            return [];
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to fetch exercises from API");
            return [];
        }
    }

    private async Task<List<ExerciseDto>> GetTrialExercisesAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("api/exercises/trial");

            if (!response.IsSuccessStatusCode)
            {
                logger.LogError("Failed to fetch trial exercises. Status code: {StatusCode}", response.StatusCode);
                return [];
            }

            return await response.Content.ReadFromJsonAsync<List<ExerciseDto>>()
                   ?? [];
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to fetch trial exercises from API");
            return [];
        }
    }

    public async Task<ExerciseDetailDto?> GetExerciseByIdAsync(int id)
    {
        try
        {
            var response = await _httpClient.GetAsync($"api/exercises/{id}");

            if (!response.IsSuccessStatusCode)
            {
                logger.LogError("Failed to fetch exercise detail. Status code: {StatusCode}", response.StatusCode);
                return null;
            }

            return await response.Content.ReadFromJsonAsync<ExerciseDetailDto>();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to fetch exercise detail from API");
            return null;
        }
    }

    public async Task<ExerciseWriteStatus> CreateExerciseAsync(ExerciseCreateDto dto)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/exercises", dto);

            if (response.StatusCode is System.Net.HttpStatusCode.Unauthorized or System.Net.HttpStatusCode.Forbidden)
            {
                return ExerciseWriteStatus.Unauthorized;
            }

            if (!response.IsSuccessStatusCode)
            {
                logger.LogError("Failed to create exercise. Status code: {StatusCode}", response.StatusCode);
                return ExerciseWriteStatus.Failed;
            }

            return ExerciseWriteStatus.Success;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create exercise");
            return ExerciseWriteStatus.Failed;
        }
    }

    public async Task<ExerciseUpdateResult> UpdateExerciseAsync(int id, ExerciseUpdateDto dto)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync($"api/exercises/{id}", dto);

            if (response.StatusCode is System.Net.HttpStatusCode.Unauthorized or System.Net.HttpStatusCode.Forbidden)
            {
                return new ExerciseUpdateResult { Status = ExerciseWriteStatus.Unauthorized };
            }

            if (!response.IsSuccessStatusCode)
            {
                logger.LogError("Failed to update exercise. Status code: {StatusCode}", response.StatusCode);
                return new ExerciseUpdateResult { Status = ExerciseWriteStatus.Failed };
            }

            return new ExerciseUpdateResult
            {
                Status = ExerciseWriteStatus.Success,
                Exercise = await response.Content.ReadFromJsonAsync<ExerciseDto>()
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to update exercise");
            return new ExerciseUpdateResult { Status = ExerciseWriteStatus.Failed };
        }
    }

    public async Task<ExerciseWriteStatus> DeleteExerciseAsync(int id)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"api/exercises/{id}");

            if (response.StatusCode is System.Net.HttpStatusCode.Unauthorized or System.Net.HttpStatusCode.Forbidden)
            {
                return ExerciseWriteStatus.Unauthorized;
            }

            if (!response.IsSuccessStatusCode)
            {
                logger.LogError("Failed to delete exercise. Status code: {StatusCode}", response.StatusCode);
                return ExerciseWriteStatus.Failed;
            }

            return ExerciseWriteStatus.Success;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to delete exercise");
            return ExerciseWriteStatus.Failed;
        }
    }

}