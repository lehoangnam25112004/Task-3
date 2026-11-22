using ConstructionProjectManagement.Models;

namespace ConstructionProjectManagement.Services;

public sealed record WbsProjectTree(
    Project Project,
    IReadOnlyList<WbsPhaseTree> Phases,
    bool IsReadOnly);

public sealed record WbsPhaseTree(
    ProjectPhase Phase,
    IReadOnlyList<WbsItemTree> RootItems);

public sealed record WbsItemTree(
    WorkItem Item,
    int Depth,
    int LinkedTaskCount,
    IReadOnlyList<WbsItemTree> Children);
