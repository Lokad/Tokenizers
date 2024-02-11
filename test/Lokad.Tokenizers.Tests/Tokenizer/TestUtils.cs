using System.Net;

namespace Lokad.Tokenizers.Tests.Tokenizer;

public class TestUtils
{
    public static string DownloadFileToCache(string url)
    {
        // Get the home directory path
        string homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        // Create the cache directory path
        string cacheDir = Path.Combine(homeDir, ".cache", ".rust_tokenizers");

        // extract file name from url
        string fileName = url.Split('/').Last();

        // Create the full path to the file
        string cachedPath = Path.Combine(cacheDir, fileName);

        // Create the cache directory if it does not exist
        if (!Directory.Exists(cacheDir))
        {
            Directory.CreateDirectory(cacheDir);
        }

        // Download the file if it does not exist
        if (!File.Exists(cachedPath))
        {
            Console.WriteLine($"Downloading {url} to {cachedPath}");
            using (var client = new WebClient())
            {
                client.DownloadFile(url, cachedPath);
            }
        }

        Console.WriteLine(cachedPath);

        return cachedPath;
    }
}
