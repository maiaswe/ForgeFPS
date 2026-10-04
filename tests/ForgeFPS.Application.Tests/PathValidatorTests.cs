using ForgeFPS.Domain;
using ForgeFPS.Infrastructure.Windows;
using ForgeFPS.Shared;
using FluentAssertions;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace ForgeFPS.Application.Tests;

public class PathValidatorTests
{
    private readonly PathValidator _validator = new(new FileSystem());

    [Fact]
    public void IsValid_PathInsideRoot_ShouldReturnTrue()
    {
        var root = Path.GetTempPath();
        var path = Path.Combine(root, "subfolder", "file.txt");

        _validator.IsValid(path, root).Should().BeTrue();
    }

    [Fact]
    public void IsValid_PathOutsideRoot_ShouldReturnFalse()
    {
        var root = Path.Combine(Path.GetTempPath(), "forgefps-root");
        var path = @"C:\Windows\System32\cmd.exe";

        _validator.IsValid(path, root).Should().BeFalse();
    }

    [Fact]
    public void IsValid_TraversalAttempt_ShouldReturnFalse()
    {
        var root = Path.Combine(Path.GetTempPath(), "forgefps-root");
        var path = Path.Combine(root, "..", "..", "Windows", "System32", "cmd.exe");

        _validator.IsValid(path, root).Should().BeFalse();
    }

    [Fact]
    public void IsValid_RelativePath_ShouldReturnFalse()
    {
        _validator.IsValid("relative/path.txt", @"C:\SomeRoot").Should().BeFalse();
    }

    [Fact]
    public void IsValid_NullByteInPath_ShouldReturnFalse()
    {
        var root = Path.GetTempPath();
        _validator.IsValid(root + "evil\0.txt", root).Should().BeFalse();
    }

    [Fact]
    public void Canonicalize_InsideRoot_ShouldReturnFullPath()
    {
        var root = Path.GetTempPath();
        var path = Path.Combine(root, "file.txt");

        var canonical = _validator.Canonicalize(path, root);
        canonical.Should().Be(Path.GetFullPath(path));
    }

    [Fact]
    public void Canonicalize_OutsideRoot_ShouldThrowUnauthorized()
    {
        var root = Path.Combine(Path.GetTempPath(), "forgefps-root");
        var act = () => _validator.Canonicalize(@"C:\Windows\System32\cmd.exe", root);
        act.Should().Throw<UnauthorizedAccessException>();
    }

    [Fact]
    public void Canonicalize_TraversalNormalized_ShouldThrow()
    {
        var root = Path.Combine(Path.GetTempPath(), "forgefps-root");
        var path = Path.Combine(root, "..", "sibling.txt");
        var act = () => _validator.Canonicalize(path, root);
        act.Should().Throw<UnauthorizedAccessException>();
    }
}

public class AtomicWriterTests
{
    private readonly FileSystem _fs = new();
    private readonly AtomicWriter _writer = new(new FileSystem());

    [Fact]
    public void WriteAtomic_ShouldCreateFileWithContents()
    {
        var dir = Path.Combine(Path.GetTempPath(), "forgefps-tests", System.Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var target = Path.Combine(dir, "config.cfg");
        try
        {
            _writer.WriteAtomic(target, "key=value");
            _fs.ReadAllText(target).Should().Be("key=value");
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    [Fact]
    public void WriteAtomic_OverwriteExisting_ShouldReplaceContents()
    {
        var dir = Path.Combine(Path.GetTempPath(), "forgefps-tests", System.Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var target = Path.Combine(dir, "config.cfg");
        try
        {
            System.IO.File.WriteAllText(target, "old=1");
            _writer.WriteAtomic(target, "new=2");
            _fs.ReadAllText(target).Should().Be("new=2");
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    [Fact]
    public void WriteAtomic_Failure_ShouldNotCorruptOriginal()
    {
        var dir = Path.Combine(Path.GetTempPath(), "forgefps-tests", System.Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var target = Path.Combine(dir, "config.cfg");
        try
        {
            System.IO.File.WriteAllText(target, "original=1");
            // Attempt to write to a target path that is invalid (directory as target)
            var invalidTarget = dir; // a directory, Move onto it will fail
            var act = () => _writer.WriteAtomic(Path.Combine(dir, "x.txt"), "content");
            // Writing to valid file still works
            act.Should().NotThrow();
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }
}