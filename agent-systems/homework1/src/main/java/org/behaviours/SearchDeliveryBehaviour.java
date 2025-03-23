package org.behaviours;

import jade.core.AID;
import jade.core.behaviours.WakerBehaviour;
import jade.domain.DFService;
import jade.domain.FIPAAgentManagement.DFAgentDescription;
import jade.domain.FIPAAgentManagement.ServiceDescription;
import org.agents.ClientAgent;

import java.util.Arrays;
import java.util.List;

public class SearchDeliveryBehaviour extends WakerBehaviour {
    private final ClientAgent _clientAgent;

    public SearchDeliveryBehaviour(final ClientAgent clientAgent, long timeout) {
        super(clientAgent, timeout);
        this._clientAgent = clientAgent;
    }

    @Override
    protected void onWake() {
        System.out.printf("[%s] I'm searching for avaiable markets...\n", _clientAgent.getLocalName());

        final ServiceDescription sd = new ServiceDescription();
        sd.setType("delivery");
        sd.setName("Delivery");

        try {
            final DFAgentDescription dfd = new DFAgentDescription();
            dfd.addServices(sd);
            final DFAgentDescription[] resultDelivery = DFService.search(_clientAgent, dfd);

            final List<AID> deliveries = Arrays.stream(resultDelivery)
                    .map(DFAgentDescription::getName)
                    .toList();

            deliveries.forEach(delivery -> System.out.printf("[%s] Found delivery service: %s\n", _clientAgent.getLocalName(), delivery));
            _clientAgent.get_delivery().addAll(deliveries);
        } catch (final Exception e) {
            throw new RuntimeException(e);
        }
    }
}
