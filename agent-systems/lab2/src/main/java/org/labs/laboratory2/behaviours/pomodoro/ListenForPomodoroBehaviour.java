package org.labs.laboratory2.behaviours.pomodoro;

import static jade.lang.acl.ACLMessage.AGREE;
import static jade.lang.acl.ACLMessage.INFORM;
import static jade.lang.acl.ACLMessage.REFUSE;
import static jade.lang.acl.ACLMessage.REQUEST;
import static java.lang.String.valueOf;
import static java.util.Objects.nonNull;

import org.labs.exceptions.InvalidMessageContentException;
import org.labs.laboratory2.agents.StudyManagerAgent;
import org.labs.laboratory2.domain.SessionLength;

import com.fasterxml.jackson.databind.ObjectMapper;
import com.fasterxml.jackson.databind.json.JsonMapper;

import jade.core.behaviours.CyclicBehaviour;
import jade.lang.acl.ACLMessage;

public class ListenForPomodoroBehaviour extends CyclicBehaviour {

	static final ObjectMapper mapper = new JsonMapper();
	private final StudyManagerAgent managerAgent;

	public ListenForPomodoroBehaviour(final StudyManagerAgent managingAgent) {
		super(managingAgent);
		this.managerAgent = managingAgent;
	}

	@Override
	public void onStart() {
		System.out.println("[ManagingAgent] Start listening for messages from StopperAgent!");
	}

	@Override
	public void action() {
		final ACLMessage msg = myAgent.receive();

		if (nonNull(msg)) {
			switch (msg.getPerformative()) {
				case INFORM -> handleInformation(msg);
				case REQUEST -> handleRequest(msg);
				default -> {
				}
			}
		} else {
			block();
		}
	}

	private void handleRequest(final ACLMessage msg) {
		System.out.println("[ManagingAgent] I received request to adjust session durations!");
		try {
			final SessionLength currentLengths = mapper.readValue(msg.getContent(), SessionLength.class);

			if (currentLengths.getShortBreakDuration() > 5000) {
				final ACLMessage refuse = msg.createReply(REFUSE);
				managerAgent.send(refuse);
			} else {
				final ACLMessage agree = msg.createReply(AGREE);
				managerAgent.send(agree);

				final SessionLength updatedSessions = SessionLength.builder()
						.studyingDuration(currentLengths.getStudyingDuration() - 1000)
						.longBreakDuration(currentLengths.getLongBreakDuration() + 1000)
						.shortBreakDuration(currentLengths.getShortBreakDuration() + 1000)
						.build();
				final ACLMessage inform = msg.createReply(INFORM);
				inform.setContent(mapper.writeValueAsString(updatedSessions));
				managerAgent.send(inform);
			}

		} catch (final Exception e) {
			throw new InvalidMessageContentException(e);
		}
	}

	private void handleInformation(final ACLMessage msg) {
		System.out.println("[ManagingAgent] I received message about finished studying cycle!");
		managerAgent.increaseCyclesNumber();
		confirmProgressReception(msg);
	}

	private void confirmProgressReception(final ACLMessage msg) {
		final ACLMessage reply = msg.createReply(INFORM);
		reply.setContent(valueOf(managerAgent.getCyclesCounter().get()));
		reply.setProtocol("SESSION_COUNT");
		managerAgent.send(reply);
	}
}
