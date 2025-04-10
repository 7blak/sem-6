package org.behaviours.delivery;

import jade.core.AID;
import jade.core.behaviours.OneShotBehaviour;
import jade.domain.DFService;
import jade.domain.FIPAAgentManagement.DFAgentDescription;
import jade.domain.FIPAAgentManagement.ServiceDescription;
import org.Util;
import org.agents.DeliveryAgent;
import org.exceptions.InvalidServiceSpecification;

import java.util.Arrays;
import java.util.List;

public class SearchMarketBehaviour extends OneShotBehaviour {
    private final DeliveryAgent _deliveryAgent;

    public SearchMarketBehaviour(final DeliveryAgent deliveryAgent) {
        super(deliveryAgent);
        this._deliveryAgent = deliveryAgent;
    }

    @Override
    public void action() {
        Util.log(_deliveryAgent, "I'm searching for available markets...");

        final ServiceDescription sd = new ServiceDescription();
        sd.setType("market");

        try {
            final DFAgentDescription dfd = new DFAgentDescription();
            dfd.addServices(sd);
            final DFAgentDescription[] resultMarkets = DFService.search(_deliveryAgent, dfd);

            final List<AID> markets = Arrays.stream(resultMarkets)
                    .map(DFAgentDescription::getName)
                    .toList();

            markets.forEach(market -> Util.log(_deliveryAgent, "Found market: " + market));
            _deliveryAgent.get_markets().addAll(markets);
        } catch (final Exception e) {
            throw new InvalidServiceSpecification(e);
        } finally {
            DeliveryAgent._latch.countDown();
            _deliveryAgent.addBehaviour(new ReceivingOrdersBehaviour(_deliveryAgent));
        }
    }
}
