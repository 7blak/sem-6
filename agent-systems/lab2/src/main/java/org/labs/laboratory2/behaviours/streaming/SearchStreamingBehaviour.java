package org.labs.laboratory2.behaviours.streaming;

import java.util.Arrays;
import java.util.List;

import org.labs.exceptions.InvalidServiceSpecification;
import org.labs.laboratory2.agents.ViewerAgent;

import jade.core.AID;
import jade.core.behaviours.WakerBehaviour;
import jade.domain.DFService;
import jade.domain.FIPAAgentManagement.DFAgentDescription;
import jade.domain.FIPAAgentManagement.ServiceDescription;
import jade.domain.FIPAException;

public class SearchStreamingBehaviour extends WakerBehaviour {

	private final ViewerAgent viewerAgent;

	public SearchStreamingBehaviour(final ViewerAgent viewerAgent) {
		super(viewerAgent, 3000);
		this.viewerAgent = viewerAgent;
	}

	@Override
	public void onWake() {
		System.out.println("[ViewerAgent] I'm searching for available streaming platforms...");

		final ServiceDescription serviceDescription = new ServiceDescription();
		serviceDescription.setType(viewerAgent.getRegion().name());
		serviceDescription.setName("Streaming");

		try {
			final DFAgentDescription agentServices = new DFAgentDescription();
			agentServices.addServices(serviceDescription);
			final DFAgentDescription[] availableServices = DFService.search(viewerAgent, agentServices);

			final List<AID> streamingPlatforms = Arrays.stream(availableServices)
					.map(DFAgentDescription::getName)
					.toList();

			streamingPlatforms.forEach(
					platform -> System.out.printf("[ViewerAgent] Found streaming platform: %s %n", platform));
			viewerAgent.getStreamingPlatforms().addAll(streamingPlatforms);
		} catch (FIPAException e) {
			throw new InvalidServiceSpecification(e);
		}
	}
}
