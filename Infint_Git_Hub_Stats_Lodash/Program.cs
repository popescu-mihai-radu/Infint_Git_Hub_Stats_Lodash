////  -> Sample programm for GitHub statics using diffrent programming approches for performance demo
////  -> This program can also be further improve to minimum exceution time by taking advantage of the Commit Diffrance in Git 
////    and so to persist the last execution statistics and to apply the computing only on the delta (diffrance from the last commit)
////  -> Also could be used as a scheduled service and if used in a WebService better to keep using it as associated simple console app (if scheduald) cause 
////  not needed to consume webserices resources
//// Author Radu Mihai POPESCU  (popescu.mihai.radu@gmail.com)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Octokit;
using System.Collections.Concurrent;
using System.Threading;

class Program
{
    static async Task Main(string[] args)
    {
        Console.Clear();
        // Replace 'token' with your actual GitHub personal access token in case of not working 
        var github = new GitHubClient(new ProductHeaderValue("Infint_Git_Hub_Stats_Lodash"))
        {
            Credentials = new Credentials("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")
        };

        // Replace 'lodash' with the desired repository owner and 'lodash' with the repository name
        var repositoryOwner = "lodash";
        var repositoryName = "lodash";
        var baseUrl = "https://raw.githubusercontent.com/" + repositoryOwner + "/" + repositoryName + "/main/";

        try
        {
            // Get repository details
            var repo = await github.Repository.Get(repositoryOwner, repositoryName);
            Console.WriteLine($"Repository: {repo.FullName}, Description: {repo.Description}");

            // Get repository statistics
            var contributors = await github.Repository.Statistics.GetContributors(repositoryOwner, repositoryName);
            Console.WriteLine($"Contributors Count: {contributors.Count}");

            // Get JavaScript/TypeScript files on all sublfolders 
            var jsTsFilesAllSubfoldersFromRoot = GetJsTsFilesTree(github, repositoryOwner, repositoryName, 10);
            Console.WriteLine($"Total JavaScript/TypeScript files Count: {jsTsFilesAllSubfoldersFromRoot.Count}");
            var letterCountForThreading = GetLetterFrequency(jsTsFilesAllSubfoldersFromRoot, baseUrl);
            Console.WriteLine();
            Console.WriteLine($"*** Multithread computing ***");
            OutputLetterFrequency(letterCountForThreading.Result);

            // Parallel call for performances 
            var letterCountForParallel = GetLetterFrequencyConcurent(jsTsFilesAllSubfoldersFromRoot, baseUrl);
            Console.WriteLine();
            Console.WriteLine($"*** Parallel computing ***");
            OutputLetterFrequencyConcurent(letterCountForParallel);

            // Get JavaScript/TypeScript files on root folder 
            var jsTsFilesRootOnly = await GetJsTsFiles(github, repositoryOwner, repositoryName);
            Console.WriteLine();
            Console.WriteLine($"*** Only Root Level ***");
            Console.WriteLine($"Total JavaScript/TypeScript files Count: {jsTsFilesRootOnly.Count}");

            // Output letter frequency in decreasing order
            var letterCount = GetLetterFrequency(jsTsFilesRootOnly);
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
        _ = Console.ReadLine();
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

    static ConcurrentDictionary<char, int> GetLetterFrequencyConcurent(List<TreeItem> files, string baseUrl)
    {
        var letterCount = new ConcurrentDictionary<char, int>();
        ParallelOptions parallelOptions = new()
        {
            MaxDegreeOfParallelism = 4
        };
        try
        {
            Parallel.ForEach(files, parallelOptions, file =>
            {
                var fileContent =  GetFileContent(baseUrl + file.Path).Result;
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
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception : {ex.Message}");
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

    static void OutputLetterFrequencyConcurent(ConcurrentDictionary<char, int> letterCount)
    {
        var sortedLetterCount = letterCount.OrderByDescending(pair => pair.Value);
        foreach (var entry in sortedLetterCount)
        {
            Console.WriteLine($"Letter: {entry.Key}, Occurrences: {entry.Value}");
        }
    }

}

