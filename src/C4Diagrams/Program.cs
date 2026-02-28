using C4Sharp.Diagrams;
using C4Sharp.Diagrams.Plantuml;
using C4Sharp.Diagrams.Themes;
using C4Diagrams.Diagrams;

var diagrams = new DiagramBuilder[]
{
    new SystemContextDiagram(),
    new ContainerDeploymentDiagram(),
    new ComponentProjectDependenciesDiagram(),
    new DomainModelDiagram(),
    new SaveDraftCommandSequence(),
    new DraftToAssignedCommandSequence(),
    new AssignedToInProgressCommandSequence(),
    new InProgressToCompleteCommandSequence()
};

var path = Path.Combine(
    AppContext.BaseDirectory, "..", "..", "..", "..", "..", "arch", "c4sharp-output");

new PlantumlContext()
    .UseDiagramImageBuilder()
    .UseDiagramSvgImageBuilder()
    .Export(path, diagrams, new DefaultTheme());
