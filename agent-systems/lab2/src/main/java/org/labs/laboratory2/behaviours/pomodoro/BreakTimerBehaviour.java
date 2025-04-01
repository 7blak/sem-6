package org.labs.laboratory2.behaviours.pomodoro;

import static jade.core.AID.ISLOCALNAME;
import static jade.lang.acl.ACLMessage.INFORM;

import org.labs.laboratory2.agents.PomodoroAgent;

import jade.core.AID;
import jade.core.behaviours.WakerBehaviour;
import jade.lang.acl.ACLMessage;

public class BreakTimerBehaviour extends WakerBehaviour {

	private static final int MAX_STUDYING_SESSIONS = 2;

	private final PomodoroAgent pomodoroAgent;

	public BreakTimerBehaviour(final PomodoroAgent a, final long timeout) {
		super(a, timeout);
		this.pomodoroAgent = a;
	}

	@Override
	protected void onWake() {
		final boolean isLongBreak = pomodoroAgent.getStudyingSessionsCount().compareAndSet(MAX_STUDYING_SESSIONS, 0);
		final int breakTime = isLongBreak ?
				pomodoroAgent.getLongBreakDuration() :
				pomodoroAgent.getShortBreakDuration();

		if (isLongBreak) {
			communicateCompletedSession();
		}
		System.out.println("[PomodoroAgent][Break] Studying session finished take a break!");
		System.out.println("[PomodoroAgent][Break] Break time: " + breakTime);

		myAgent.addBehaviour(new StudyingTimerBehaviour(pomodoroAgent, breakTime));
	}

	private void communicateCompletedSession() {
		final ACLMessage message = new ACLMessage(INFORM);
		message.setContent("Cycle finished.");
		message.addReceiver(new AID("Manager", ISLOCALNAME));

		myAgent.send(message);
		myAgent.addBehaviour(new ConfirmationListenerBehaviour());
		myAgent.addBehaviour(new UnexpectedMsgListenerBehaviour());
	}
}
