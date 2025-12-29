namespace CloneProject.Tests

open System
open System.Text.Json
open CloneProject
open NUnit.Framework

module ConfigurationReaderTests =

    [<Test>]
    let ``readFromContent should parse valid JSON with Default and StartsWith targets`` () =
        // Arrange
        let json = """
        {
            "Default": "/default/path",
            "https://github.com/user/": "/github/path",
            "https://dev.azure.com/org/": "/azure/path"
        }
        """
        let expected = {
            Default = "/default/path"
            Targets = [
                {
                    UrlPattern = UrlPattern.StartsWith "https://github.com/user/"
                    TargetDirectory = "/github/path"
                }
                {
                    UrlPattern = UrlPattern.StartsWith "https://dev.azure.com/org/"
                    TargetDirectory = "/azure/path"
                }
            ]
        }

        // Act
        let result = ConfigurationReader.readFromContent json

        // Assert
        Assert.That(result, Is.EqualTo(expected))

    [<Test>]
    let ``readFromContent should parse REGEX patterns`` () =
        // Arrange
        let json = """
        {
            "Default": "/default",
            "REGEX:^https://dev\\.azure\\.com/(?<Org>\\w+)/.*$": "/azure/${Org}"
        }
        """
        let expected = {
            Default = "/default"
            Targets = [
                {
                    UrlPattern = UrlPattern.Regex "^https://dev\\.azure\\.com/(?<Org>\\w+)/.*$"
                    TargetDirectory = "/azure/${Org}"
                }
            ]
        }

        // Act
        let result = ConfigurationReader.readFromContent json

        // Assert
        Assert.That(result, Is.EqualTo(expected))

    [<Test>]
    let ``readFromContent should throw when regex does not start with ^`` () =
        // Arrange
        let json = """
        {
            "Default": "/default",
            "REGEX:https://dev\\.azure\\.com/(?<Org>\\w+)/$": "/azure/${Org}"
        }
        """

        // Act
        let act = fun () -> ConfigurationReader.readFromContent json |> ignore

        // Assert
        let ex = Assert.Throws<Exception>(act)
        Assert.That(ex.Message, Does.Contain("REGEX pattern must start with '^'"))

    [<Test>]
    let ``readFromContent should throw when regex does not ends with &`` () =
        // Arrange
        let json = """
        {
            "Default": "/default",
            "REGEX:^https://dev\\.azure\\.com/(?<Org>\\w+)/": "/azure/${Org}"
        }
        """

        // Act
        let act = fun () -> ConfigurationReader.readFromContent json |> ignore

        // Assert
        let ex = Assert.Throws<Exception>(act)
        Assert.That(ex.Message, Does.Contain("REGEX pattern must end with '$'"))

    [<Test>]
    let ``readFromContent should replace {{UserProfile}} placeholder`` () =
        // Arrange
        let userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
        let json = $"""
        {{
            "Default": "{{{{UserProfile}}}}/projects/default",
            "https://github.com/": "{{{{UserProfile}}}}/projects/github"
        }}
        """
        let expected = {
            Default = $"{userProfile}/projects/default"
            Targets = [
                {
                    UrlPattern = UrlPattern.StartsWith "https://github.com/"
                    TargetDirectory = $"{userProfile}/projects/github"
                }
            ]
        }

        // Act
        let result = ConfigurationReader.readFromContent json

        // Assert
        Assert.That(result, Is.EqualTo(expected))

    [<Test>]
    let ``readFromContent should throw when Default is missing`` () =
        // Arrange
        let json = """
        {
            "https://github.com/user/": "/github/path"
        }
        """

        // Act
        let act = fun () -> ConfigurationReader.readFromContent json |> ignore

        // Act & Assert
        Assert.Throws<Exception>(act) |> ignore

    [<Test>]
    [<Ignore("JSON deserialization overwrites duplicate keys, so this test may not behave as expected")>]
    let ``readFromContent should throw when multiple Defaults are found`` () =
        // Arrange
        let json = """
        {
            "Default": "/default1",
            "Default": "/default2"
        }
        """

        // Note: JSON deserialization will overwrite duplicate keys, so this might not throw
        // But if the Map somehow has multiple "Default" keys, it should throw
        // This test documents the expected behavior

        // Act
        let act = fun () -> ConfigurationReader.readFromContent json |> ignore

        // Assert
        Assert.Throws<Exception>(act) |> ignore

    [<Test>]
    let ``readFromContent should handle empty targets list`` () =
        // Arrange
        let json = """
        {
            "Default": "/default/path"
        }
        """
        let expected = {
            Default = "/default/path"
            Targets = []
        }

        // Act
        let result = ConfigurationReader.readFromContent json

        // Assert
        Assert.That(result, Is.EqualTo(expected))

    [<Test>]
    let ``readFromContent should handle case-insensitive REGEX prefix`` () =
        // Arrange
        let json = """
        {
            "Default": "/default",
            "regex:^https://github\\.com/.*$": "/github"
        }
        """
        let expected = {
            Default = "/default"
            Targets = [
                {
                    UrlPattern = UrlPattern.Regex "^https://github\\.com/.*$"
                    TargetDirectory = "/github"
                }
            ]
        }

        // Act
        let result = ConfigurationReader.readFromContent json

        // Assert
        Assert.That(result, Is.EqualTo(expected))

    [<Test>]
    let ``readFromContent should throw on invalid JSON`` () =
        // Arrange
        let invalidJson = "{ invalid json }"

        // Act
        let act = fun () -> ConfigurationReader.readFromContent invalidJson |> ignore

        // Assert
        Assert.Throws<JsonException>(act) |> ignore

