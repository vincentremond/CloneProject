namespace CloneProject.Tests

open NUnit.Framework

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
    [<TestCase("https://dev.azure.com/MyOrganisation/MyProjectName/_git/MyRepoName", ExpectedResult = "/some/dir1")>]
    [<TestCase("https://github.com/vincentremond/Pinicola.FSharp.git", ExpectedResult = "/some/dir2")>]
    [<TestCase("https://unknown.com/some/repo.git", ExpectedResult = "/some/default")>]
    let deductTargetDirectory url =
        let uri = System.Uri(url)

        let config =
            Map [
                ("Default", "/some/default")
                ("https://dev.azure.com/MyOrganisation/MyProjectName/", "/some/dir1")
                ("https://github.com/vincentremond/", "/some/dir2")
            ]

        Program.deductTargetDirectory config uri |> Option.toObj
