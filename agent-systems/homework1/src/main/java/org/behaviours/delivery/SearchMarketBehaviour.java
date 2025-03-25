package org.behaviours.delivery;

import jade.core.AID;
import jade.core.behaviours.WakerBehaviour;
import jade.domain.DFService;
import jade.domain.FIPAAgentManagement.DFAgentDescription;
import jade.domain.FIPAAgentManagement.ServiceDescription;
import org.agents.DeliveryAgent;
import org.exceptions.InvalidServiceSpecification;

import java.util.Arrays;
import java.util.List;

public class SearchMarketBehaviour extends WakerBehaviour {
    private final DeliveryAgent _deliveryAgent;

    public SearchMarketBehaviour(final DeliveryAgent deliveryAgent, long timeout) {
        super(deliveryAgent, timeout);
        this._deliveryAgent = deliveryAgent;
    }

    @Override
    protected void onWake() {
        System.out.printf("[%s] I'm searching for available markets...\n", _deliveryAgent.getLocalName());

        final ServiceDescription sd = new ServiceDescription();
        sd.setType("market");

        try {
            final DFAgentDescription dfd = new DFAgentDescription();
            dfd.addServices(sd);
            final DFAgentDescription[] resultMarkets = DFService.search(_deliveryAgent, dfd);

            final List<AID> markets = Arrays.stream(resultMarkets)
                    .map(DFAgentDescription::getName)
                    .toList();

            markets.forEach(market -> System.out.printf("[%s] Found market: %s\n", _deliveryAgent.getLocalName(), market));
            _deliveryAgent.get_markets().addAll(markets);
        } catch (final Exception e) {
            throw new InvalidServiceSpecification(e);
        } finally {
            _deliveryAgent.addBehaviour(new ReceiveOrderBehaviour(_deliveryAgent));
        }
    }
}
