using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using ScoringService.Domain.Entities;
using ScoringService.Domain.Enums;
using ScoringService.Domain.ValueObjects;
using Xunit;

namespace ScoringService.Tests.Domain
{
    public class SessionMonitorTests
    {
        private static SessionMonitor CreateMonitor()
            => SessionMonitor.Create(Guid.NewGuid(), Guid.NewGuid(), "Sesión de prueba");

        [Fact]
        public void Create_ShouldStartPendingWithoutTeams()
        {
            var monitor = CreateMonitor();

            monitor.Status.Should().Be(SessionMonitorStatus.Pending);
            monitor.Teams.Should().BeEmpty();
            monitor.AcceptsEvidence().Should().BeFalse();
        }

        [Fact]
        public void Create_WithEmptySessionId_ShouldThrow()
        {
            Action act = () => SessionMonitor.Create(Guid.Empty, Guid.NewGuid(), "x");
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void RegisterTeam_ShouldAddTeam()
        {
            var monitor = CreateMonitor();
            var teamId = Guid.NewGuid();

            monitor.RegisterTeam(teamId, new TeamCode("Alfa"));

            monitor.Teams.Should().ContainSingle(t => t.TeamId == teamId);
            monitor.HasTeam(teamId).Should().BeTrue();
        }

        [Fact]
        public void RegisterTeam_Twice_ShouldBeIdempotent()
        {
            var monitor = CreateMonitor();
            var teamId = Guid.NewGuid();

            monitor.RegisterTeam(teamId, new TeamCode("Alfa"));
            monitor.RegisterTeam(teamId, new TeamCode("Alfa"));

            monitor.Teams.Should().HaveCount(1);
        }

        [Fact]
        public void Activate_ShouldEnableEvidenceAndStoreSubStages()
        {
            var monitor = CreateMonitor();
            var sub = Guid.NewGuid();

            monitor.Activate(new List<Guid> { sub, sub });

            monitor.Status.Should().Be(SessionMonitorStatus.Active);
            monitor.AcceptsEvidence().Should().BeTrue();
            monitor.ActiveSubStageIds.Should().ContainSingle().Which.Should().Be(sub);
            monitor.IsSubStageActive(sub).Should().BeTrue();
        }

        [Fact]
        public void Pause_ShouldStopAcceptingEvidence()
        {
            var monitor = CreateMonitor();
            monitor.Activate(new List<Guid>());

            monitor.Pause();

            monitor.Status.Should().Be(SessionMonitorStatus.Paused);
            monitor.AcceptsEvidence().Should().BeFalse();
        }

        [Fact]
        public void Finish_ShouldClearActiveSubStages()
        {
            var monitor = CreateMonitor();
            monitor.Activate(new List<Guid> { Guid.NewGuid() });

            monitor.Finish();

            monitor.Status.Should().Be(SessionMonitorStatus.Finished);
            monitor.ActiveSubStageIds.Should().BeEmpty();
        }

        [Fact]
        public void Cancel_ShouldClearActiveSubStages()
        {
            var monitor = CreateMonitor();
            monitor.Activate(new List<Guid> { Guid.NewGuid() });

            monitor.Cancel();

            monitor.Status.Should().Be(SessionMonitorStatus.Cancelled);
            monitor.ActiveSubStageIds.Should().BeEmpty();
        }

        [Fact]
        public void TransitionStage_ShouldReplaceActiveSubStages()
        {
            var monitor = CreateMonitor();
            monitor.Activate(new List<Guid> { Guid.NewGuid() });
            var next = Guid.NewGuid();

            monitor.TransitionStage(new List<Guid> { next });

            monitor.ActiveSubStageIds.Should().ContainSingle().Which.Should().Be(next);
        }
    }
}
