package org.labs.laboratory1;

import static org.labs.JADEEngine.runAgent;
import static org.labs.JADEEngine.runGUI;

import java.util.concurrent.ExecutionException;
import java.util.concurrent.ExecutorService;
import java.util.concurrent.Executors;

import org.labs.exceptions.JadePlatformInitializationException;

import jade.core.Profile;
import jade.core.ProfileImpl;
import jade.core.Runtime;
import jade.wrapper.ContainerController;

public class Engine {

	private static final ExecutorService jadeExecutor = Executors.newCachedThreadPool();

	public static void main(String[] args) {
		final Runtime runtime = Runtime.instance();
		final Profile profile = new ProfileImpl();

		try {
			final ContainerController container = jadeExecutor.submit(() -> runtime.createMainContainer(profile)).get();

			runGUI(container);
			runAgent(container, "Agent1", "FirstAgent", "laboratory1");
		} catch (final InterruptedException | ExecutionException e) {
			throw new JadePlatformInitializationException(e);
		}
	}
}
