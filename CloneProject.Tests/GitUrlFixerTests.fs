namespace CloneProject.Tests

open CloneProject
open NUnit.Framework

module GitUrlFixerTests =

    [<Test>]
    [<TestCase("https://xxxx@dev.azure.com/yyy/xxx/_git/lorem", ExpectedResult = "https://dev.azure.com/yyy/xxx/_git/lorem")>]
    [<TestCase("https://xxx.visualstudio.com/", ExpectedResult = "https://dev.azure.com/xxx/")>]
    [<TestCase("https://dev.azure.com/xxx/DefaultCollection/", ExpectedResult = "https://dev.azure.com/xxx/")>]
    [<TestCase("https://MyOrganisation.visualstudio.com/DefaultCollection/MyProjectName/_git/MyRepoName",
               ExpectedResult = "https://dev.azure.com/MyOrganisation/MyProjectName/_git/MyRepoName")>]
    [<TestCase("https://github.com/vincentremond/Pinicola.FSharp.git", ExpectedResult = "https://github.com/vincentremond/Pinicola.FSharp.git")>]
    let ``fix should return expected result`` url = GitUrlFixer.fix url
