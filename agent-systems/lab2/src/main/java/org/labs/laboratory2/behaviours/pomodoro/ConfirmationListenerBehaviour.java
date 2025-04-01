package org.labs.laboratory2.behaviours.pomodoro;

import static jade.lang.acl.MessageTemplate.MatchProtocol;
import static java.lang.Integer.parseInt;
import static java.lang.System.currentTimeMillis;
import static java.util.Objects.nonNull;

import org.labs.laboratory2.agents.PomodoroAgent;

import jade.lang.acl.ACLMessage;
import jade.proto.states.MsgReceiver;

public class ConfirmationListenerBehaviour extends MsgReceiver {

	public ConfirmationListenerBehaviour() {
		super();
		this.template = MatchProtocol("SESSION_COUNT");
		this.deadline = currentTimeMillis() + 5000;
	}

	@Override
	protected void handleMessage(final ACLMessage msg) {
		if (nonNull(msg)) {
			final int sessionNo = parseInt(msg.getContent());
			System.out.printf("[PomodoroAgent] Manager confirmed update (session %d). %n", sessionNo);

			if (sessionNo % 2 == 0) {
				System.out.println("[PomodoroAgent] You're getting tired - I want to adjust session durations!");
				myAgent.addBehaviour(new DurationRequestBehaviour((PomodoroAgent) myAgent));
			}
		}
	}
}
