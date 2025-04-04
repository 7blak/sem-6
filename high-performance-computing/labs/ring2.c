#include <stdio.h>
#include <stdlib.h>
#include <mpi.h>

#define MSG_SIZE 100
#define TAG_FORWARD 1
#define TAG_BACKWARD 2
#define MPI_OPERATIONS 4

int main(int argc, char* argv[])
{
    int myRank, numProcs, fwdDest, bckDest, fwdSource, bckSource;

    char fwdSend[MSG_SIZE], fwdRecv[MSG_SIZE];
    char bckSend[MSG_SIZE], bckRecv[MSG_SIZE];

    for (int i = 0; i < MSG_SIZE; i++)
    {
        fwdSend[i] = '+';
        bckSend[i] = '-';
    }

    MPI_Status status[MPI_OPERATIONS];
    MPI_Request request[MPI_OPERATIONS];
    
    MPI_Init(&argc, &argv);
    MPI_Comm_rank(MPI_COMM_WORLD, &myRank);
    MPI_Comm_size(MPI_COMM_WORLD, &numProcs);

    if (myRank == 0)
    {
        printf("The number of processors in this run is %d.\n", numProcs);
    }

    fwdDest = (myRank + 1) % numProcs;
    fwdSource = myRank == 0 ? numProcs - 1 : myRank - 1;
    bckDest = myRank == 0 ? numProcs - 1: myRank - 1;
    bckSource = (myRank + 1) % numProcs;

    printf("Process %d | FWD: send %d, receive %d, BCK: send %d, receive %d\n", myRank, fwdDest, fwdSource, bckDest, bckSource);

    MPI_Isend(fwdSend, MSG_SIZE, MPI_CHAR, fwdDest, TAG_FORWARD, MPI_COMM_WORLD, &request[0]);
    MPI_Isend(bckSend, MSG_SIZE, MPI_CHAR, bckDest, TAG_BACKWARD, MPI_COMM_WORLD, &request[1]);

    MPI_Irecv(fwdRecv, MSG_SIZE, MPI_CHAR, fwdSource, TAG_FORWARD, MPI_COMM_WORLD, &request[2]);
    MPI_Irecv(bckRecv, MSG_SIZE, MPI_CHAR, bckSource, TAG_BACKWARD, MPI_COMM_WORLD, &request[3]);

    MPI_Waitall(MPI_OPERATIONS, request, status);
    printf("Process %d | MPI_Waitall successfull\n", myRank);

    if (myRank == 0)
    {
        int fwdErr = 0, bckErr = 0;
        for (int i = 0; i < MSG_SIZE; i++)
        {
            if (fwdRecv[i] != '+') fwdErr++;
            if (bckRecv[i] != '-') bckErr++;
        }

        printf("Process %d | fwdErr: %d, bckErr: %d\n", myRank, fwdErr, bckErr);
    }

    MPI_Finalize();
    return 0;
}