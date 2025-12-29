namespace CloneProject.Tests

open System.Text.RegularExpressions
open CloneProject
open NUnit.Framework

module TargetDirectoryServiceTests =

    [<Test>]
    [<TestCase("https://dev.azure.com/MyOrganisation1/MyProjectName/_git/MyRepoName", ExpectedResult = "/some/dir1")>]
    [<TestCase("https://github.com/vincentremond/Pinicola.FSharp.git", ExpectedResult = "/some/dir2")>]
    [<TestCase("https://unknown.com/some/repo.git", ExpectedResult = "/some/default")>]
    [<TestCase("https://dev.azure.com/MyOrganisation2/SomeProject/_git/Repo", ExpectedResult = "/some/dir3/SomeProject")>]
    let ``find should return expected directory`` url =
        let uri = System.Uri(url)

        let config: Configuration = {
            Default = "/some/default"
            Targets = [
                {
                    UrlPattern = UrlPattern.StartsWith "https://dev.azure.com/MyOrganisation1/MyProjectName/"
                    TargetDirectory = "/some/dir1"
                }
                {
                    UrlPattern = UrlPattern.StartsWith "https://github.com/vincentremond/"
                    TargetDirectory = "/some/dir2"
                }
                {
                    UrlPattern = UrlPattern.Regex @"^https://dev\.azure\.com/MyOrganisation2/(?<ProjectName>\w+)/.+$"
                    TargetDirectory = "/some/dir3/${ProjectName}"
                }
            ]
        }

        TargetDirectoryService.find config uri
