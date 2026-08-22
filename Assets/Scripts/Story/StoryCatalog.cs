public static class StoryCatalog
{
    public const int TotalFloorStoryCount = 10;
    public const string Floor10ClearStoryId = "floor_10_clear";
    public const string Floor10KeywordId = "love";
    public const string Floor10KeywordName = "사랑";
    public const string Floor10KeywordDescription =
        "누군가를 소중하게 생각하는 마음";
    public const string Floor10ChapterDescription =
        "되찾은 첫 번째 단어";

    public static bool IsFloorStory(string storyId)
    {
        return !string.IsNullOrEmpty(storyId) &&
            storyId.StartsWith("floor_") &&
            storyId.EndsWith("_clear");
    }
}
