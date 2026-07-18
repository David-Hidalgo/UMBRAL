using System;
using FluentAssertions;
using Xunit;
using LiveSessionService.Domain.Entities;
using LiveSessionService.Tests.Support;

namespace LiveSessionService.Tests.Domain
{
    public class LiveSessionTimerTests
    {
        private static readonly DateTime Now = new(2026, 7, 13, 10, 0, 0, DateTimeKind.Utc);

        private static LiveSession ActiveSession(int maxTimeMinutes, DateTime now)
        {
            var session = LiveSession.Create(Guid.NewGuid(), Guid.NewGuid(), "Misión", "Sesión", null, maxTimeMinutes);
            session.RegisterTeamWithPlayer("Alfa");
            session.StartPreparation();
            session.Activate(now);
            return session;
        }

        [Fact]
        public void Create_ShouldStoreMaxTimeMinutes()
        {
            var session = LiveSession.Create(Guid.NewGuid(), Guid.NewGuid(), "Misión", "Sesión", null, 45);

            session.MaxTimeMinutes.Should().Be(45);
        }

        [Fact]
        public void Create_WithoutMaxTime_ShouldDefaultToZero()
        {
            var session = LiveSession.Create(Guid.NewGuid(), Guid.NewGuid(), "Misión", "Sesión", null);

            session.MaxTimeMinutes.Should().Be(0);
            session.EndsAt.Should().BeNull();
        }

        [Fact]
        public void Activate_WithNow_ShouldSetDeadline()
        {
            var session = ActiveSession(30, Now);

            session.EndsAt.Should().Be(Now.AddMinutes(30));
            session.RemainingSecondsOnPause.Should().BeNull();
        }

        [Fact]
        public void Activate_WithoutNow_ShouldNotSetDeadline()
        {
            var session = LiveSession.Create(Guid.NewGuid(), Guid.NewGuid(), "Misión", "Sesión", null, 30);
            session.RegisterTeamWithPlayer("Alfa");
            session.StartPreparation();

            session.Activate(); // sin reloj (compatibilidad con tests que no ejercitan el temporizador)

            session.EndsAt.Should().BeNull();
        }

        [Fact]
        public void Pause_ShouldFreezeRemainingTime()
        {
            var session = ActiveSession(30, Now); // EndsAt = 10:30

            session.Pause(Now.AddMinutes(10)); // a los 10 min → quedan 20

            session.RemainingSecondsOnPause.Should().Be(20 * 60);
        }

        [Fact]
        public void Resume_ShouldRestoreDeadlineFromRemaining()
        {
            var session = ActiveSession(30, Now);
            session.Pause(Now.AddMinutes(10)); // quedan 20 min

            var resumeAt = Now.AddMinutes(25); // se reanuda más tarde
            session.Resume(resumeAt);

            session.EndsAt.Should().Be(resumeAt.AddMinutes(20));
            session.RemainingSecondsOnPause.Should().BeNull();
        }

        [Fact]
        public void PauseThenResume_ShouldPreserveTotalRemaining()
        {
            var session = ActiveSession(30, Now);

            session.Pause(Now.AddMinutes(5)); // quedan 25 min
            var resumeAt = Now.AddMinutes(100); // reanuda mucho después
            session.Resume(resumeAt);

            var remainingMinutes = (session.EndsAt!.Value - resumeAt).TotalMinutes;
            remainingMinutes.Should().BeApproximately(25, 0.01);
        }
    }
}
