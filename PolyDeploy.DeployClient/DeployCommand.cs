﻿namespace PolyDeploy.DeployClient
{
    using System.IO.Abstractions;

    using Spectre.Console.Cli;
    using Spectre.Console;

    public class DeployCommand : AsyncCommand<DeployInput>
    {
        private readonly IFileSystem fileSystem;
        private readonly IDeployer deployer;

        public DeployCommand(IDeployer deployer, IFileSystem fileSystem)
        {
            this.deployer = deployer;
            this.fileSystem = fileSystem;
        }

        public override async Task<int> ExecuteAsync(CommandContext context, DeployInput input)
        {
            var exitCode = await this.deployer.StartAsync(input);
            return (int)exitCode;
        }

        public override ValidationResult Validate(CommandContext context, DeployInput settings)
        {
            if (!string.IsNullOrWhiteSpace(settings.PackagesDirectoryPath) && !this.fileSystem.Directory.Exists(settings.PackagesDirectoryPath))
            {
                return ValidationResult.Error("--packages-directory must be a valid path");
            }

            if (!Uri.TryCreate(settings.TargetUri, UriKind.Absolute, out _))
            {
                return ValidationResult.Error("--target-uri must be a valid URI");
            }

            if (settings.InstallationStatusTimeout < 0)
            {
                return ValidationResult.Error("--installation-status-timeout must be non-negative");
            }

            return ValidationResult.Success();
        }
    }
}
