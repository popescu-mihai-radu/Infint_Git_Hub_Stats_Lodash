using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Octokit;

class Program
{
    static async Task Main(string[] args)
    {

        // Replace 'YourAccessToken' with your actual GitHub personal access token
        var github = new GitHubClient(new ProductHeaderValue("YourAppName"))
        {
            Credentials = new Credentials("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")
        };

        // Replace 'lodash' with the desired repository owner and 'lodash' with the repository name
        var repositoryOwner = "lodash";
        var repositoryName = "lodash";
        var baseUrl = "https://raw.githubusercontent.com/" + repositoryOwner + "/" + repositoryName+ "/main/";

        try
        {
            // Get repository details
            var repo = await github.Repository.Get(repositoryOwner, repositoryName);
            Console.WriteLine($"Repository: {repo.FullName}, Description: {repo.Description}");

            // Get repository statistics
            var contributors = await github.Repository.Statistics.GetContributors(repositoryOwner, repositoryName);
            Console.WriteLine($"Contributors Count: {contributors.Count}");

            // Get JavaScript/TypeScript files on all sublfolders 
            var jsTsFiles2 = GetJsTsFilesTree(github, repositoryOwner, repositoryName,3);
            Console.WriteLine($"Total JavaScript/TypeScript files Count: {jsTsFiles2.Count}");
            var letterCount2 = GetLetterFrequency(jsTsFiles2, baseUrl);
            OutputLetterFrequency(letterCount2.Result);

            // Get JavaScript/TypeScript files on root folder 
            var jsTsFiles = await GetJsTsFiles(github, repositoryOwner, repositoryName);
            Console.WriteLine($"Total JavaScript/TypeScript files Count: {jsTsFiles.Count}");

            // Output letter frequency in decreasing order
            var letterCount = GetLetterFrequency(jsTsFiles);
            OutputLetterFrequency(letterCount.Result);
        }
        catch (NotFoundException)
        {
            Console.WriteLine($"Repository {repositoryOwner}/{repositoryName} not found.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred: {ex.Message}");
        }
        Console.ReadLine();
    }

    static List<TreeItem> GetJsTsFilesTree(GitHubClient github, string owner, string repo, int maxNoOfRecords)
    {
        var allFilesTrees = github.Git.Tree.GetRecursive(owner, repo, "main").Result;
        Console.WriteLine($"Total files Count: {allFilesTrees.Tree.Count}");

        return allFilesTrees.Tree.Where(content => content.Type == TreeType.Blob &&
                              (content.Path.EndsWith(".js") || content.Path.EndsWith(".ts"))).Take(maxNoOfRecords)
            .ToList();

    }
    static async Task<List<RepositoryContent>> GetJsTsFiles(GitHubClient github, string owner, string repo)
    {
        var repositoryContents = await github.Repository.Content.GetAllContents(owner, repo); // without subfolders
        Console.WriteLine($"Total files Count: {repositoryContents.Count}");

        return repositoryContents
            .Where(content => content.Type == ContentType.File &&
                              (content.Name.EndsWith(".js") || content.Name.EndsWith(".ts")))
            .ToList();
    }

    static async Task<Dictionary<char, int>> GetLetterFrequency(List<TreeItem> files, string baseUrl)
    {
        var letterCount = new Dictionary<char, int>();

        foreach (var file in files)
        {
            var fileContent = await GetFileContent(baseUrl + file.Path);
            foreach (char letter in fileContent)
            {
                if (char.IsLetter(letter))
                {
                    var lowercaseLetter = char.ToLower(letter);
                    if (letterCount.ContainsKey(lowercaseLetter))
                    {
                        letterCount[lowercaseLetter]++;
                    }
                    else
                    {
                        letterCount[lowercaseLetter] = 1;
                    }
                }
            }
        }

        return letterCount;
    }

    static async Task<Dictionary<char, int>> GetLetterFrequency(List<RepositoryContent> files)
    {
        var letterCount = new Dictionary<char, int>();

        foreach (var file in files)
        {
            var fileContent = await GetFileContent(file);
            foreach (char letter in fileContent)
            {
                if (char.IsLetter(letter))
                {
                    var lowercaseLetter = char.ToLower(letter);
                    if (letterCount.ContainsKey(lowercaseLetter))
                    {
                        letterCount[lowercaseLetter]++;
                    }
                    else
                    {
                        letterCount[lowercaseLetter] = 1;
                    }
                }
            }
        }

        return letterCount;
    }

    static async Task<string> GetFileContent(RepositoryContent file)
    {
        using (var httpClient = new HttpClient())
        {
            var fileContentUrl = file.DownloadUrl;
            return await httpClient.GetStringAsync(fileContentUrl);
        }
    }
    static async Task<string> GetFileContent(string urlFile)
    {
        using (var httpClient = new HttpClient())
        {
            var fileContentUrl = urlFile;
            return await httpClient.GetStringAsync(fileContentUrl);
        }
    }

    static void OutputLetterFrequency(Dictionary<char, int> letterCount)
    {
        var sortedLetterCount = letterCount.OrderByDescending(pair => pair.Value);
        foreach (var entry in sortedLetterCount)
        {
            Console.WriteLine($"Letter: {entry.Key}, Occurrences: {entry.Value}");
        }
    }
}

