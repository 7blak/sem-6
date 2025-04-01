package org.labs.laboratory2.agents;

import java.util.List;

import org.labs.exceptions.InvalidServiceSpecification;
import org.labs.laboratory2.domain.Genre;
import org.labs.laboratory2.domain.Region;

import jade.core.Agent;
import jade.domain.DFService;
import jade.domain.FIPAAgentManagement.DFAgentDescription;
import jade.domain.FIPAAgentManagement.ServiceDescription;
import jade.domain.FIPAException;

@SuppressWarnings("unchecked")
public class StreamingPlatformAgent extends Agent {

	private Region region;
	private boolean supportsLiveEvents;
	private List<Genre> genres;

	@Override
	protected void setup() {
		System.out.printf("[%s] I'm ready to register my services! %n", getLocalName());

		final Object[] args = getArguments();
		region = (Region) args[0];
		supportsLiveEvents = (boolean) args[1];
		genres = (List<Genre>) args[2];

		registerStreamingService();
	}

	@Override
	protected void takeDown() {
		try {
			DFService.deregister(this);
		} catch (final FIPAException e) {
			throw new InvalidServiceSpecification(e);
		}
	}

	private void registerStreamingService() {
		final ServiceDescription serviceDescription = new ServiceDescription();
		serviceDescription.setType(region.name());
		serviceDescription.setName("Streaming");
		serviceDescription.setOwnership(this.getLocalName());

		try {
			final DFAgentDescription agentServices = new DFAgentDescription();
			agentServices.addServices(serviceDescription);
			DFService.register(this, agentServices);
		} catch (final FIPAException e) {
			throw new InvalidServiceSpecification(e);
		}
	}
}
