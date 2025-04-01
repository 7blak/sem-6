package org.labs.laboratory2.agents;

import java.util.ArrayList;
import java.util.List;

import org.labs.laboratory2.behaviours.streaming.SubscribeStreamingBehaviour;
import org.labs.laboratory2.domain.Genre;
import org.labs.laboratory2.domain.Region;

import jade.core.AID;
import jade.core.Agent;
import lombok.Getter;
import lombok.Setter;

@Getter
public class ViewerAgent extends Agent {

	private Genre favouriteGenre;
	private Region region;
	@Setter
	private List<AID> streamingPlatforms;

	@Override
	protected void setup() {
		System.out.println("[ViewerAgent] I'm ready to watch my favourite shows!");

		final Object[] args = getArguments();
		favouriteGenre = (Genre) args[0];
		region = (Region) args[1];
		streamingPlatforms = new ArrayList<>();

		//addBehaviour(new SearchStreamingBehaviour(this));
		addBehaviour(new SubscribeStreamingBehaviour(this));
	}
}
