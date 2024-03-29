namespace PolyDeploy.DeployClient
{
    using System.Threading.Tasks;

    public interface IDeployer
    {
        Task<ExitCode> StartAsync(DeployInput options);
    }
}