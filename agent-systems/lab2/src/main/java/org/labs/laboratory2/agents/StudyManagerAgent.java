package org.labs.laboratory2.agents;

import java.util.concurrent.atomic.AtomicInteger;

import org.labs.laboratory2.behaviours.pomodoro.ListenForPomodoroBehaviour;

import jade.core.Agent;
import lombok.Getter;

@Getter
public class StudyManagerAgent extends Agent {

	private final AtomicInteger cyclesCounter = new AtomicInteger(0);

	@Override
	protected void setup() {
		System.out.println("[StudyManagerAgent] I'm initialized! I'm ready to monitor studying progress.");
		addBehaviour(new ListenForPomodoroBehaviour(this));
	}

	public void increaseCyclesNumber() {
		cyclesCounter.incrementAndGet();
		System.out.printf("[ManagingAgent] Congratulation on finishing %d round of studying cycles! %n",
				cyclesCounter.get());
	}
}
