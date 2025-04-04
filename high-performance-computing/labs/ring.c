#include <stdio.h>
#include <stdlib.h>
#include <mpi.h>

#define ARRAY_SIZE 10000   
#define TAG_FORWARD 1     
#define TAG_BACKWARD 2    
 
int main(int argc, char *argv[]) {
    int myrank, numProcs;
    MPI_Init(&argc, &argv);
    MPI_Comm_rank(MPI_COMM_WORLD, &myrank);
    MPI_Comm_size(MPI_COMM_WORLD, &numProcs);

    int forwardDest = (myrank + 1) % numProcs;
    int forwardSrc  = (myrank == 0) ? numProcs - 1 : myrank - 1;
    int backwardDest = (myrank == 0) ? numProcs - 1 : myrank - 1;
    int backwardSrc  = (myrank + 1) % numProcs;
 
    char sendForward[ARRAY_SIZE], recvForward[ARRAY_SIZE];
    char sendBackward[ARRAY_SIZE], recvBackward[ARRAY_SIZE];
 
    for (int i = 0; i < ARRAY_SIZE; i++) {
        sendForward[i] = '+';
        sendBackward[i] = '-';
    }
 
    MPI_Request request[4];
    MPI_Status status[4];

    MPI_Isend(sendForward, ARRAY_SIZE, MPI_CHAR, forwardDest, TAG_FORWARD, MPI_COMM_WORLD, &request[0]);
 
    MPI_Isend(sendBackward, ARRAY_SIZE, MPI_CHAR, backwardDest, TAG_BACKWARD, MPI_COMM_WORLD, &request[1]);
 
    MPI_Irecv(recvForward, ARRAY_SIZE, MPI_CHAR, forwardSrc, TAG_FORWARD, MPI_COMM_WORLD, &request[2]);
 
    MPI_Irecv(recvBackward, ARRAY_SIZE, MPI_CHAR, backwardSrc, TAG_BACKWARD, MPI_COMM_WORLD, &request[3]);

    MPI_Waitall(4, request, status);
 
    if (myrank == 0) {
        int forwardErrors = 0, backwardErrors = 0;
        for (int i = 0; i < ARRAY_SIZE; i++) {
            if (recvForward[i] != '+') forwardErrors++;
            if (recvBackward[i] != '-') backwardErrors++;
        }
        if (forwardErrors == 0 && backwardErrors == 0) {
            printf("Processor 0 successfully validated all messages. Program terminating normally.\n");
        } else {
            printf("Processor 0 found errors: %d forward, %d backward.\n", forwardErrors, backwardErrors);
        }
    }
 
    MPI_Finalize();
    return 0;
}
 