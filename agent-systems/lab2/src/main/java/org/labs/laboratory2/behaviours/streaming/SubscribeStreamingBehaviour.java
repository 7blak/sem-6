package org.labs.laboratory2.behaviours.streaming;

import static java.util.function.Predicate.not;
import static java.util.stream.Collectors.toMap;

import java.util.Arrays;
import java.util.List;
import java.util.Map;

import org.labs.exceptions.InvalidServiceSpecification;
import org.labs.laboratory2.agents.ViewerAgent;

import jade.core.AID;
import jade.domain.DFService;
import jade.domain.FIPAAgentManagement.DFAgentDescription;
import jade.domain.FIPAAgentManagement.ServiceDescription;
import jade.domain.FIPAException;
import jade.lang.acl.ACLMessage;
import jade.proto.SubscriptionInitiator;

public class SubscribeStreamingBehaviour extends SubscriptionInitiator {

	private final ViewerAgent viewerAgent;

	public SubscribeStreamingBehaviour(final ViewerAgent a) {
		super(a, createSubscription(a));
		viewerAgent = a;
	}

	private static ACLMessage createSubscription(final ViewerAgent viewerAgent) {
		System.out.println("[ViewerAgent] I'm subscribing streaming platforms services.");

		final ServiceDescription serviceDescription = new ServiceDescription();
		serviceDescription.setType(viewerAgent.getRegion().name());
		serviceDescription.setName("Streaming");
		final DFAgentDescription agentServices = new DFAgentDescription();
		agentServices.addServices(serviceDescription);
		return DFService.createSubscriptionMessage(viewerAgent, viewerAgent.getDefaultDF(), agentServices, null);
	}

	@Override
	protected void handleInform(final ACLMessage inform) {
		try {
			final Map<AID, Boolean> newPlatforms = Arrays.stream(DFService.decodeNotification(inform.getContent()))
					.collect(toMap(DFAgentDescription::getName, desc -> desc.getAllServices().hasNext()));
			final List<AID> addedPlatforms = newPlatforms.entrySet().stream()
					.filter(Map.Entry::getValue)
					.map(Map.Entry::getKey)
					.toList();
			final List<AID> removedPlatforms = newPlatforms.entrySet().stream()
					.filter(not(Map.Entry::getValue))
					.map(Map.Entry::getKey)
					.toList();

			if (!addedPlatforms.isEmpty()) {
				System.out.println("[ViewerAgent] New streaming platforms found!");
				addedPlatforms.forEach(
						platform -> System.out.printf("[ViewerAgent] Found streaming platform: %s %n", platform));
				viewerAgent.getStreamingPlatforms().addAll(addedPlatforms);
			}

			if (!removedPlatforms.isEmpty()) {
				System.out.println("[ViewerAgent] Streaming platforms removed their services!");
				addedPlatforms.forEach(
						platform -> System.out.printf("[ViewerAgent] Streaming platform: %s removed its service %n",
								platform));
				viewerAgent.getStreamingPlatforms().removeAll(removedPlatforms);
			}

		} catch (final FIPAException e) {
			throw new InvalidServiceSpecification(e);
		}
	}
}
