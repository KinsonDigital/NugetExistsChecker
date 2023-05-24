﻿// <copyright file="ActionOutputServiceTests.cs" company="KinsonDigital">
// Copyright (c) KinsonDigital. All rights reserved.
// </copyright>

namespace PackageMonsterTests.Services;

using System.IO.Abstractions;
using FluentAssertions;
using PackageMonster.Exceptions;
using PackageMonster.Services;
using PackageMonsterTests.Helpers;
using Moq;

public class ActionOutputServiceTests
{
    private readonly Mock<IEnvVarService> mockEnvVarService;
    private readonly Mock<IFile> mockFile;
    private readonly Mock<IGitHubConsoleService> mockConsoleService;

    /// <summary>
    /// Initializes a new instance of the <see cref="ActionOutputServiceTests"/> class.
    /// </summary>
    public ActionOutputServiceTests()
    {
        this.mockEnvVarService = new Mock<IEnvVarService>();
        this.mockFile = new Mock<IFile>();
        this.mockConsoleService = new Mock<IGitHubConsoleService>();
    }

    #region Constructor Tests
    [Fact]
    public void Ctor_WithNullEnvVarServiceParam_ThrowsException()
    {
        // Arrange & Act
        var act = () =>
        {
            _ = new ActionOutputService(null, Mock.Of<IFile>(), Mock.Of<IGitHubConsoleService>());
        };

        // Assert
        act.Should()
            .Throw<ArgumentNullException>()
            .WithMessage("The parameter must not be null. (Parameter 'envVarService')");
    }

    [Fact]
    public void Ctor_WithFileParam_ThrowsException()
    {
        // Arrange & Act
        var act = () =>
        {
            _ = new ActionOutputService(Mock.Of<IEnvVarService>(), null, null);
        };

        // Assert
        act.Should()
            .Throw<ArgumentNullException>()
            .WithMessage("The parameter must not be null. (Parameter 'file')");
    }

    [Fact]
    public void Ctor_WithConsoleServiceParam_ThrowsException()
    {
        // Arrange & Act
        var act = () =>
        {
            _ = new ActionOutputService(Mock.Of<IEnvVarService>(), Mock.Of<IFile>(), null);
        };

        // Assert
        act.Should()
            .Throw<ArgumentNullException>()
            .WithMessage("The parameter must not be null. (Parameter 'consoleService')");
    }
    #endregion

    #region Method Tests
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void SetOutputValue_WithNullOrEmptyOutputName_ThrowsException(string name)
    {
        // Arrange
        var sut = CreateSystemUnderTest();

        // Act
        var act = () => sut.SetOutputValue(name, It.IsAny<string>());

        // Assert
        act.Should()
            .Throw<NullOrEmptyStringException>()
            .WithMessage("The parameter 'name' must not be null or empty.");
    }

    [Fact]
    public void SetOutputValue_WhenOutputPathNotSpecified_LogWarning()
    {
        // Arrange
        const string outputPath = "";
        this.mockEnvVarService
            .Setup(m => m.GetEnvironmentVariable(It.IsAny<string>(), It.IsAny<EnvironmentVariableTarget>()))
            .Returns(outputPath);
        var sut = CreateSystemUnderTest();

        // Act
        sut.SetOutputValue("test-output", "test-value");

        // Assert
        this.mockConsoleService.VerifyOnce(m => m.WriteLine("WARNING: The environment variable 'GITHUB_OUTPUT' was not specified."));
    }

    [Fact]
    public void SetOutputValue_WhenOutputPathDoesNotExist_ThrowsException()
    {
        // Arrange
        const string outputPath = "test-path";
        this.mockEnvVarService
            .Setup(m => m.GetEnvironmentVariable(It.IsAny<string>(), It.IsAny<EnvironmentVariableTarget>()))
            .Returns(outputPath);
        this.mockFile.Setup(m => m.Exists(It.IsAny<string?>())).Returns(false);
        var sut = CreateSystemUnderTest();

        // Act
        var act = () => sut.SetOutputValue("test-output", "test-value");

        // Assert
        act.Should().Throw<FileNotFoundException>()
            .WithMessage("The GitHub output file was not found.");
    }

    [Fact]
    public void SetOutputValue_WhenInvoked_SetsOutputValue()
    {
        // Arrange
        var expected = @"other-output=other-value
test-output=test-value
".ReplaceLineEndings(Environment.NewLine);

        const string outputPath = "test-path";
        var lines = new[]
        {
            "other-output=other-value",
        };
        this.mockEnvVarService
            .Setup(m => m.GetEnvironmentVariable(It.IsAny<string>(), It.IsAny<EnvironmentVariableTarget>()))
            .Returns(outputPath);
        this.mockFile.Setup(m => m.ReadAllLines(It.IsAny<string>()))
            .Returns<string>(_ => lines);
        this.mockFile.Setup(m => m.Exists(It.IsAny<string?>())).Returns(true);
        var sut = CreateSystemUnderTest();

        // Act
        sut.SetOutputValue("test-output", "test-value");

        // Assert
        this.mockEnvVarService
            .VerifyOnce(m => m.GetEnvironmentVariable("GITHUB_OUTPUT", EnvironmentVariableTarget.Process));
        this.mockFile.VerifyOnce(m => m.Exists(outputPath));
        this.mockFile.VerifyOnce(m => m.ReadAllLines(outputPath));
        this.mockFile.VerifyOnce(m => m.WriteAllText(outputPath, expected));
    }
    #endregion

    /// <summary>
    /// Creates a new instance of <see cref="ActionOutputService"/> for the purpose of testing.
    /// </summary>
    /// <returns>The instance to test.</returns>
    private ActionOutputService CreateSystemUnderTest() =>
        new (this.mockEnvVarService.Object, this.mockFile.Object, this.mockConsoleService.Object);
}
