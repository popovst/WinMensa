using GraphQL;
using GraphQL.Client;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.Newtonsoft;
using Newtonsoft.Json;

namespace WinMensa.Core;

public record Price(
    double Employee,
    double Guest,
    double Pupil,
    double Student);

public record NutritionData(
    double Energy,
    double Protein,
    double Carbohydrates,
    double Sugar,
    double Fat,
    double SaturatedFat,
    double Salt);

public record EnvironmentInfo(
    double AverageRating,
    double Co2Rating,
    double Co2Value,
    double WaterRating,
    double WaterValue,
    double AnimalWelfareRating,
    double RainforestRating,
    double MaxRating);

public record Statistics(
    string? LastServed,
    string? NextServed,
    double Frequency,
    bool New);

public record Ratings(
    double AverageRating,
    double? PersonalRating,
    int RatingsCount);

public record MealImage(
    string Id,
    string Url,
    double Rank,
    bool PersonalDownvote,
    bool PersonalUpvote,
    int Downvotes,
    int Upvotes)
{
    public Uri ImageUri => new Uri(Url);
}

public record Side(
    string Id,
    string Name,
    string MealType,
    string[] Additives,
    string[] Allergens,
    Price Price,
    NutritionData? NutritionData,
    EnvironmentInfo? EnvironmentInfo);

public record Meal(
    string Id,
    string Name,
    string MealType,
    Price Price,
    string[] Allergens,
    string[] Additives,
    NutritionData? NutritionData,
    EnvironmentInfo? EnvironmentInfo,
    Statistics Statistics,
    Ratings Ratings,
    MealImage[] Images,
    Side[] Sides);

public record Canteen(string Id, string Name);

public record Line(
    string Id,
    string Name,
    Canteen Canteen,
    Meal[] Meals);

public record GetCanteenDateResponse(
    [property: JsonProperty("getCanteen")] GetCanteenDateData GetCanteen);

public record GetCanteenDateData(Line[] Lines);

public record GetDefaultCanteenResponse(
    [property: JsonProperty("getCanteens")] Canteen[] GetCanteens);

public class Query
{
    const string GQL_QUERY = @"query Canteen($canteenId: ID!, $date: String!) {
    getCanteen(canteenId: $canteenId) {
        name
        lines {
            name
            meals(date: $date) {
                id
                name
                mealType
                price {
                    student
                }
                sides {
                    id
                    name
                    price {
                        student
                    }
                }
                allergens
                additives
                images {
                    url
                    id
                    rank
                    upvotes
                    downvotes
                }
                nutritionData {
                    energy
                    protein
                    carbohydrates
                    sugar
                    fat
                    saturatedFat
                    salt
                }
            }
        }
    }
}";

    private static readonly GraphQLHttpClient gqlClient = new(
        "https://api.mensa-ka.de/",
        new NewtonsoftJsonSerializer(),
        new HttpClient { Timeout = TimeSpan.FromSeconds(10) });

    public static String MOLTKE_ID = "8d1af6fc-547e-4078-a7f7-47948304e9fd";

    public static async Task<GetCanteenDateData?> GetCanteenData()
    {
        var menuRequest = new GraphQLRequest
        {
            Query = GQL_QUERY,
            OperationName = "Canteen",
            Variables = new
            {
                canteenId = MOLTKE_ID,
                date = DateTime.Now.ToString("yyyy-MM-dd")
            }
        };
        var response = await gqlClient.SendQueryAsync<GetCanteenDateResponse>(menuRequest);

        return response.Data?.GetCanteen;
    }
}
