using GoLive.Generator.ApiClientGenerator.Data;

namespace GoLive.Generator.ApiClientGenerator.Generation;

interface IControllerGenerator
{
    public void Generate(ControllerRoute route);
}