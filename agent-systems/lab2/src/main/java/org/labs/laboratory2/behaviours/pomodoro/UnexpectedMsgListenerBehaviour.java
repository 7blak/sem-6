package org.labs.laboratory2.behaviours.pomodoro;

import static jade.core.AID.ISLOCALNAME;
import static jade.lang.acl.MessageTemplate.MatchSender;
import static java.lang.System.currentTimeMillis;
import static java.util.Objects.nonNull;

import jade.core.AID;
import jade.lang.acl.ACLMessage;
import jade.proto.states.MsgReceiver;

public class UnexpectedMsgListenerBehaviour extends MsgReceiver {

	public UnexpectedMsgListenerBehaviour() {
		super();
		this.template = MatchSender(new AID("NonExistingAgent", ISLOCALNAME));
		this.deadline = currentTimeMillis() + 5000;
	}

	@Override
	protected void handleMessage(final ACLMessage msg) {
		final String info = nonNull(msg) ?
				"[PomodoroAgent] I received this message - something is wrong!" :
				"[PomodoroAgent] Ups, I put the wrong sender - it's all good!";

		System.out.println(info);
	}
}
