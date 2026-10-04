using ForgeFPS.Domain;
using ForgeFPS.Games.CS2;
using FluentAssertions;
using Moq;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace ForgeFPS.Games.CS2.Tests;

public class Cs2ConfigParserTests
{
    [Fact]
    public void Parse_WithComments_ShouldPreserveStructure()
    {
        var dir = Path.Combine(Path.GetTempPath(), "forgefps-cs2-test");
        Directory.CreateDirectory(dir);
        var filePath = Path.Combine(dir, "client.cfg");

        try
        {
            var content = @"// Video settings
mat_queue_mode -1
mat_postprocess_enable 0 // Disable post-processing
unknown_setting foo
";
            File.WriteAllText(filePath, content);

            var result = Cs2ConfigParser.Parse(filePath);

            result.Settings.Should().HaveCount(3);
            result.Settings[0].Key.Should().Be("mat_queue_mode");
            result.Settings[0].Value.Should().Be("-1");
            result.Settings[1].Key.Should().Be("mat_postprocess_enable");
            result.Settings[1].Value.Should().Be("0");
            result.Settings[1].CommentAfter.Should().Be("Disable post-processing");
            result.Settings[2].Key.Should().Be("unknown_setting");
            result.Settings[2].IsKnownSetting.Should().BeFalse();
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    [Fact]
    public void ApplyProfile_ShouldMergeWithoutDeletingUnknownKeys()
    {
        var dir = Path.Combine(Path.GetTempPath(), "forgefps-cs2-test");
        Directory.CreateDirectory(dir);
        var filePath = Path.Combine(dir, "client.cfg");

        try
        {
            File.WriteAllText(filePath, "my_bind \"w\" \"forward\"\nmat_postprocess_enable 1\n");
            var original = Cs2ConfigParser.Parse(filePath);

            var profile = new Dictionary<string, string>
            {
                { "mat_postprocess_enable", "0" }
            };

            var updated = Cs2ConfigParser.ApplyProfile(original, profile);
            var text = Cs2ConfigParser.RebuildText(updated);

            // my_bind should be preserved
            text.Should().Contain("my_bind");
            // mat_postprocess_enable should be updated
            text.Should().Contain("mat_postprocess_enable 0");
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    [Fact]
    public void Parse_NonExistentFile_ShouldThrow()
    {
        var act = () => Cs2ConfigParser.Parse(Path.Combine(Path.GetTempPath(), "nonexistent.cfg"));
        act.Should().Throw<ArgumentException>();
    }
}