namespace CloneProject.Tests

open System.Text.RegularExpressions
open NUnit.Framework
open Program

module UnitTests =

    [<Test>]
    [<TestCase("https://xxxx@dev.azure.com/yyy/xxx/_git/lorem", ExpectedResult = "https://dev.azure.com/yyy/xxx/_git/lorem")>]
    [<TestCase("https://xxx.visualstudio.com/", ExpectedResult = "https://dev.azure.com/xxx/")>]
    [<TestCase("https://dev.azure.com/xxx/DefaultCollection/", ExpectedResult = "https://dev.azure.com/xxx/")>]
    [<TestCase("https://MyOrganisation.visualstudio.com/DefaultCollection/MyProjectName/_git/MyRepoName",
               ExpectedResult = "https://dev.azure.com/MyOrganisation/MyProjectName/_git/MyRepoName")>]
    [<TestCase("https://github.com/vincentremond/Pinicola.FSharp.git", ExpectedResult = "https://github.com/vincentremond/Pinicola.FSharp.git")>]
    let fixGitUrl url = Program.fixGitUrl url

    [<Test>]
    [<TestCase("https://dev.azure.com/MyOrganisation1/MyProjectName/_git/MyRepoName", ExpectedResult = "/some/dir1")>]
    [<TestCase("https://github.com/vincentremond/Pinicola.FSharp.git", ExpectedResult = "/some/dir2")>]
    [<TestCase("https://unknown.com/some/repo.git", ExpectedResult = "/some/default")>]
    [<TestCase("https://dev.azure.com/MyOrganisation2/SomeProject/_git/Repo", ExpectedResult = "/some/dir3/SomeProject")>]
    let deductTargetDirectory url =
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
                    UrlPattern = UrlPattern.Regex (Regex(@"^https://dev\.azure\.com/MyOrganisation2/(?<ProjectName>\w+)/.+$"))
                    TargetDirectory = "/some/dir3/${ProjectName}"
                }
            ]
        }

        deductTargetDirectory config uri
