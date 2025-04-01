package org.labs.laboratory2.behaviours.pomodoro;

import org.labs.laboratory2.agents.PomodoroAgent;

import jade.core.behaviours.WakerBehaviour;

public class StudyingTimerBehaviour extends WakerBehaviour {

	private final PomodoroAgent pomodoroAgent;

	public StudyingTimerBehaviour(final PomodoroAgent a) {
		super(a, 0);
		this.pomodoroAgent = a;
	}

	public StudyingTimerBehaviour(final PomodoroAgent a, final long timeout) {
		super(a, timeout);
		this.pomodoroAgent = a;
	}

	@Override
	protected void onWake() {
		final int sessionsCount = pomodoroAgent.getStudyingSessionsCount().incrementAndGet();

		System.out.println("[PomodoroAgent][Studying] Start studying session");
		System.out.println("[PomodoroAgent][Studying] Current studying session number: " + sessionsCount);

		myAgent.addBehaviour(new BreakTimerBehaviour(pomodoroAgent, pomodoroAgent.getStudyingDuration()));
	}
}
