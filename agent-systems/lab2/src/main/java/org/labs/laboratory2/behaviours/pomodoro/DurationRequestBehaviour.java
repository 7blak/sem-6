package org.labs.laboratory2.behaviours.pomodoro;

import static jade.core.AID.ISLOCALNAME;
import static jade.lang.acl.ACLMessage.REQUEST;

import java.io.IOException;

import org.labs.exceptions.InvalidMessageContentException;
import org.labs.laboratory2.agents.PomodoroAgent;
import org.labs.laboratory2.domain.SessionLength;

import com.fasterxml.jackson.databind.ObjectMapper;
import com.fasterxml.jackson.databind.json.JsonMapper;

import jade.core.AID;
import jade.lang.acl.ACLMessage;
import jade.proto.AchieveREInitiator;

public class DurationRequestBehaviour extends AchieveREInitiator {

	static final ObjectMapper mapper = new JsonMapper();
	final PomodoroAgent pomodoroAgent;

	public DurationRequestBehaviour(final PomodoroAgent a) {
		super(a, createRequestForNewDuration(a));
		this.pomodoroAgent = a;
	}

	private static ACLMessage createRequestForNewDuration(final PomodoroAgent pomodoroAgent) {
		final SessionLength currentSessionsLength = SessionLength.builder()
				.longBreakDuration(pomodoroAgent.getLongBreakDuration())
				.shortBreakDuration(pomodoroAgent.getShortBreakDuration())
				.studyingDuration(pomodoroAgent.getStudyingDuration())
				.build();

		try {
			final ACLMessage msg = new ACLMessage(REQUEST);
			msg.setContent(mapper.writeValueAsString(currentSessionsLength));
			msg.addReceiver(new AID("Manager", ISLOCALNAME));
			return msg;
		} catch (final IOException e) {
			throw new InvalidMessageContentException(e);
		}
	}

	@Override
	protected void handleAgree(final ACLMessage agree) {
		System.out.println("[PomodoroAgent] Manager agreed to adjust session durations.");
	}

	@Override
	protected void handleRefuse(final ACLMessage refuse) {
		System.out.println("[PomodoroAgent] I cannot adjust session durations further - let's finish!.");
		pomodoroAgent.doDelete();
	}

	@Override
	protected void handleInform(final ACLMessage inform) {
		try {
			final SessionLength sessionLength = mapper.readValue(inform.getContent(), SessionLength.class);

			System.out.println("[PomodoroAgent] Received new session durations. Updating the information.");
			pomodoroAgent.setStudyingDuration(sessionLength.getStudyingDuration());
			pomodoroAgent.setLongBreakDuration(sessionLength.getLongBreakDuration());
			pomodoroAgent.setShortBreakDuration(sessionLength.getShortBreakDuration());

		} catch (final Exception e) {
			throw new InvalidMessageContentException(e);
		}
	}
}
