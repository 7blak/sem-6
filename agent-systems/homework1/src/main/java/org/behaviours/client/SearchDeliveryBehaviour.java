package org.behaviours.client;

import jade.core.AID;
import jade.core.behaviours.WakerBehaviour;
import jade.domain.DFService;
import jade.domain.FIPAAgentManagement.DFAgentDescription;
import jade.domain.FIPAAgentManagement.ServiceDescription;
import org.Util;
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
        Util.log(_clientAgent, "Searching for avaiable delivery services...");

        final ServiceDescription sd = new ServiceDescription();
        sd.setType("delivery");

        try {
            final DFAgentDescription dfd = new DFAgentDescription();
            dfd.addServices(sd);
            final DFAgentDescription[] resultDelivery = DFService.search(_clientAgent, dfd);

            final List<AID> deliveries = Arrays.stream(resultDelivery)
                    .map(DFAgentDescription::getName)
                    .toList();

            deliveries.forEach(delivery -> Util.log(_clientAgent, "Found delivery service: " + delivery));
            _clientAgent.get_delivery().addAll(deliveries);
        } catch (final Exception e) {
            throw new RuntimeException(e);
        } finally {
            _clientAgent.addBehaviour(new SendOrderBehaviour(_clientAgent));
            _clientAgent.addBehaviour(new SelectOfferBehaviour(_clientAgent, 8000));
        }
    }
}
