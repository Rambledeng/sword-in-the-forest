using System;
using System.Collections.Generic;

namespace SwordInTheForest.Run
{
    /// <summary>One step of a run: a room to clear, or a choice of upgrades.</summary>
    public sealed class RunStep
    {
        private RunStep(RunStepKind kind, RoomPlan? room, IReadOnlyList<Upgrade> choices)
        {
            Kind = kind;
            Room = room;
            Choices = choices;
        }

        public RunStepKind Kind { get; }
        public RoomPlan? Room { get; }
        /// <summary>The three upgrades offered (empty for a room).</summary>
        public IReadOnlyList<Upgrade> Choices { get; }

        public static RunStep ForRoom(RoomPlan room) => new RunStep(RunStepKind.Room, room, Array.Empty<Upgrade>());
        public static RunStep ForUpgrade(IReadOnlyList<Upgrade> choices) => new RunStep(RunStepKind.Upgrade, null, choices);
    }
}
