package org.labs.laboratory2.agents;

import java.util.concurrent.atomic.AtomicInteger;

import org.labs.laboratory2.behaviours.pomodoro.StudyingTimerBehaviour;

import jade.core.Agent;
import lombok.Getter;
import lombok.Setter;

@Getter
@Setter
public class PomodoroAgent extends Agent {

	private int longBreakDuration;
	private int shortBreakDuration;
	private int studyingDuration;

	private boolean isLongBreak;
	private AtomicInteger studyingSessionsCount;

	@Override
	protected void setup() {
		System.out.println("[PomodoroAgent] I'm initialized! Let's start with studying session.");

		this.isLongBreak = false;
		this.studyingSessionsCount = new AtomicInteger(0);
		this.longBreakDuration = 5000;
		this.shortBreakDuration = 2000;
		this.studyingDuration = 5000;

		addBehaviour(new StudyingTimerBehaviour(this));
	}
}
