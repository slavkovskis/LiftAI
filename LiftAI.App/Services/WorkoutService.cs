using System.Net.Http.Json;
using LiftAI.Shared.Models.Dtos.Workout;

namespace LiftAI.App.Services;

public class WorkoutService(IHttpClientFactory httpClientFactory, ILogger<WorkoutService> logger)
{
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient("Api");

    public async Task<List<WorkoutDto>> GetAllWorkoutsAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("api/workouts");
            
            if (!response.IsSuccessStatusCode)
            {
                logger.LogError("Failed to fetch workouts. Status code: {StatusCode}", response.StatusCode);
                return [];
            }

            return await response.Content.ReadFromJsonAsync<List<WorkoutDto>>()
                   ?? [];
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to fetch workouts from API");
            return [];
        }
    }

    public async Task<WorkoutDetailDto?> CreateWorkoutAsync(WorkoutCreateDto dto)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/workouts", dto);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogError("Failed to create workout. Status code: {StatusCode}", response.StatusCode);
                return null;
            }
            
            return await response.Content.ReadFromJsonAsync<WorkoutDetailDto>();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create workout");
            return null;
        }
    }

    public async Task<bool> CompleteGuestWorkoutAsync(WorkoutCreateDto dto)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/workouts/guest-complete", dto);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogError("Failed to complete guest workout. Status code: {StatusCode}", response.StatusCode);
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to complete guest workout");
            return false;
        }
    }

    public async Task<WorkoutDetailDto?> GetWorkoutDetailAsync(int id)
    {
        try
        {
            var response = await _httpClient.GetAsync($"api/workouts/{id}");

            if (!response.IsSuccessStatusCode)
            {
                logger.LogError("Failed to fetch workout. Status code: {StatusCode}", response.StatusCode);
                return null;
            }
            
            return await response.Content.ReadFromJsonAsync<WorkoutDetailDto>();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to fetch workout from API");
            return null;
        }
    }

    public async Task<WorkoutDetailDto?> UpdateWorkoutAsync(int id, WorkoutCreateDto dto)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync($"api/workouts/{id}", dto);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogError("Failed to update workout. Status code: {StatusCode}", response.StatusCode);
                return null;
            }

            return await response.Content.ReadFromJsonAsync<WorkoutDetailDto>();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to update workout");
            return null;
        }
    }

    public async Task<bool> DeleteWorkoutAsync(int id)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"api/workouts/{id}");

            if (!response.IsSuccessStatusCode)
            {
                logger.LogError("Failed to remove workout. Status code: {StatusCode}", response.StatusCode);
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to remove workout from API");
            return false;
        }
    }
}