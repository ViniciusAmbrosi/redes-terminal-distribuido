#include <sys/socket.h>
#include <netinet/in.h>
#include <sys/time.h>
#include <stdio.h>
#include <unistd.h>
#include <string.h>
#include <stdlib.h>

#include <netinet/sctp.h>
#include <sys/types.h>

#define ECHOMAX 1024

char listenedPort[ECHOMAX];
char targetIp[ECHOMAX];
char targetPort[ECHOMAX];

int inboundConnectionEstablished[100];
int knownInboundConnections = 0;

int outboundConnectionEstablished = 0;

void *handleInboundConnection(void *threadReference)
{
	int threadReferenceValue = *(int *)threadReference;
	inboundConnectionEstablished[knownInboundConnections++] = threadReferenceValue;

	do
	{
		char receivedMessage[ECHOMAX];

		sctp_recvmsg(threadReferenceValue, &receivedMessage, sizeof(receivedMessage), NULL, 0, 0, 0);
		printf("\nReceived from child --- \n %s\n", receivedMessage);

		int controlChar = 0;

		if (receivedMessage[ECHOMAX - 1] == '0')
		{
			receivedMessage[ECHOMAX - 1] = '\0';

			FILE *fp = malloc(sizeof(FILE));
			char fullOutput[ECHOMAX];
			char path[ECHOMAX];

			fp = popen(receivedMessage, "r");
			if (fp == NULL)
			{
				printf("Failed to run command\n");
				exit(1);
			}

			while (fgets(path, sizeof(path), fp) != NULL)
			{
				strcat(fullOutput, path);
				printf("%s", path);
			}

			pclose(fp);

			receivedMessage[strcspn(receivedMessage, "\n")] = 0;
			receivedMessage[ECHOMAX - 1] = '8';

			sctp_sendmsg(threadReferenceValue, &fullOutput, sizeof(fullOutput), NULL, 0, 0, 0, 0, 0, 0);		
		}
		if (controlChar > 0 && controlChar < 8){
			receivedMessage[ECHOMAX - 1] = '9';
			sctp_sendmsg(outboundConnectionEstablished, &receivedMessage, sizeof(receivedMessage), NULL, 0, 0, 0, 0, 0, 0);
		}
		if(receivedMessage[ECHOMAX - 1] == '8')
		{
			receivedMessage[ECHOMAX - 1] = '\0';

			FILE *fp;
			char fullOutput[ECHOMAX];
			char path[ECHOMAX];

			fp = popen(receivedMessage, "r");
			if (fp == NULL)
			{
				printf("Failed to run command\n");
				exit(1);
			}

			while (fgets(path, sizeof(path), fp) != NULL)
			{
				strcat(fullOutput, path);
				printf("%s", path);
			}

			pclose(fp);

			receivedMessage[strcspn(receivedMessage, "\n")] = 0;
			receivedMessage[ECHOMAX - 1] = '1';

			printf("Respondendo comando pro pai");
			sctp_sendmsg(outboundConnectionEstablished, &fullOutput, sizeof(fullOutput), NULL, 0, 0, 0, 0, 0, 0);
		}
	} while (1);
}

void *handleOutboundConnection(void *threadReference)
{
	int threadReferenceValue = *(int *)threadReference;
	outboundConnectionEstablished = threadReferenceValue;

	do
	{
		char receivedMessage[ECHOMAX];

		sctp_recvmsg(threadReferenceValue, &receivedMessage, sizeof(receivedMessage), NULL, 0, 0, 0);
		printf("\nReceived from parent --- \n %s\n", receivedMessage);

		int controlChar = 0;

		if(receivedMessage[ECHOMAX - 1] == '0')
		{
			receivedMessage[ECHOMAX - 1] = '\0';

			FILE *fp = malloc(sizeof(FILE));
			char fullOutput[ECHOMAX];
			char path[ECHOMAX];

			fp = popen(receivedMessage, "r");
			if (fp == NULL)
			{
				printf("Failed to run command\n");
				exit(1);
			}

			while (fgets(path, sizeof(path), fp) != NULL)
			{
				strcat(fullOutput, path);
				printf("%s", path);
			}

			pclose(fp);

			receivedMessage[strcspn(receivedMessage, "\n")] = 0;
			receivedMessage[ECHOMAX - 1] = '8';

			sctp_sendmsg(threadReferenceValue, &fullOutput, sizeof(fullOutput), NULL, 0, 0, 0, 0, 0, 0);
		}
		if (controlChar > 0 && controlChar < 8){
			receivedMessage[ECHOMAX - 1] = '9';
			
			for (int i = 0; i < knownInboundConnections; i++)
			{
				usleep(500000);
				sctp_sendmsg(inboundConnectionEstablished[i], &receivedMessage, sizeof(receivedMessage), NULL, 0, 0, 0, 0, 0, 0);
			}
		}
		if(receivedMessage[ECHOMAX - 1] == '8')
		{
			receivedMessage[ECHOMAX - 1] = '\0';

			FILE *fp;
			char fullOutput[ECHOMAX];
			char path[ECHOMAX];

			fp = popen(receivedMessage, "r");
			if (fp == NULL)
			{
				printf("Failed to run command\n");
				exit(1);
			}

			while (fgets(path, sizeof(path), fp) != NULL)
			{
				strcat(fullOutput, path);
				printf("%s", path);
			}

			pclose(fp);

			receivedMessage[strcspn(receivedMessage, "\n")] = 0;
			receivedMessage[ECHOMAX - 1] = '1';

			printf("Respondendo comando pro filho");
			for (int i = 0; i < knownInboundConnections; i++)
			{
				usleep(500000);
				sctp_sendmsg(inboundConnectionEstablished[i], &fullOutput, sizeof(fullOutput), NULL, 0, 0, 0, 0, 0, 0);
			}
		}
	} while (1);
}

void *monitorTerminalForCommands(void *parameter)
{
	char linha[ECHOMAX];
	do
	{
		fgets(linha, ECHOMAX, stdin);
		linha[strcspn(linha, "\n")] = 0;
		linha[ECHOMAX - 1] = '0';

		printf("Mandando comando pro pai %s", linha);
		
		usleep(500000);
		sctp_sendmsg(outboundConnectionEstablished, &linha, sizeof(linha), NULL, 0, 0, 0, 0, 0, 0);

		for (int i = 0; i < knownInboundConnections; i++)
		{
			printf("Mandando comando pro filho %s", linha);

			usleep(500000);
			sctp_sendmsg(inboundConnectionEstablished[i], &linha, sizeof(linha), NULL, 0, 0, 0, 0, 0, 0);
		}

	} while (1);
}

void *monitorIncomingConnections(void *parameter)
{
	int loc_sockfd, loc_newsockfd, tamanho;

	struct sockaddr_in loc_addr = {
		.sin_family = AF_INET,
		.sin_addr.s_addr = INADDR_ANY,
		.sin_port = htons(atoi(listenedPort))};

	struct sctp_initmsg initmsg = {
		.sinit_num_ostreams = 5,
		.sinit_max_instreams = 5,
		.sinit_max_attempts = 4,
	};

	loc_sockfd = socket(AF_INET, SOCK_STREAM, IPPROTO_SCTP);

	if (loc_sockfd < 0)
	{
		perror("Criando stream socket");
		exit(1);
	}

	if (bind(loc_sockfd, (struct sockaddr *)&loc_addr, sizeof(struct sockaddr)) < 0)
	{
		perror("Ligando stream socket");
		exit(1);
	}

	if (setsockopt(loc_sockfd, IPPROTO_SCTP, SCTP_INITMSG, &initmsg, sizeof(initmsg)) < 0)
	{
		perror("setsockopt(initmsg)");
		exit(1);
	}

	do
	{
		listen(loc_sockfd, initmsg.sinit_max_instreams);
		printf("> pronto para receber conexão inicial ou nova \n");

		tamanho = sizeof(struct sockaddr_in);
		loc_newsockfd = accept(loc_sockfd, (struct sockaddr *)&loc_addr, &tamanho);

		pthread_t threadId;
		pthread_create(&threadId, NULL, handleInboundConnection, &loc_newsockfd);
	} while (1);

	close(loc_sockfd);
	close(loc_newsockfd);
}

int main(int argc, char *argv[])
{
	char inputDeConfiguracao[ECHOMAX];

	printf("> Provide peer port for incoming connections \n");
	fgets(inputDeConfiguracao, ECHOMAX, stdin);
	memcpy(listenedPort, inputDeConfiguracao, sizeof(inputDeConfiguracao));
	listenedPort[strcspn(listenedPort, "\n")] = 0;

	printf("> Wants to connect to remote peer? \n");
	fgets(inputDeConfiguracao, ECHOMAX, stdin);
	inputDeConfiguracao[strcspn(inputDeConfiguracao, "\n")] = 0;

	if (strcmp(inputDeConfiguracao, "S") == 0)
	{
		printf("> Target IP \n");
		fgets(inputDeConfiguracao, ECHOMAX, stdin);
		memcpy(targetIp, inputDeConfiguracao, sizeof(inputDeConfiguracao));
		targetIp[strcspn(targetIp, "\n")] = 0;

		printf("> Target PORT \n");
		fgets(inputDeConfiguracao, ECHOMAX, stdin);
		memcpy(targetPort, inputDeConfiguracao, sizeof(inputDeConfiguracao));
		targetPort[strcspn(targetPort, "\n")] = 0;

		int rem_sockfd;
		char linha[ECHOMAX];

		struct sockaddr_in rem_addr = {
			.sin_family = AF_INET,
			.sin_addr.s_addr = inet_addr(targetIp),
			.sin_port = htons(atoi(targetPort)),
		};

		rem_sockfd = socket(AF_INET, SOCK_STREAM, IPPROTO_SCTP);
		if (rem_sockfd < 0)
		{
			perror("Criando stream socket");
			exit(1);
		}

		printf("> Conectando no servidor '%s:%s'\n", targetIp, targetPort);

		if (connect(rem_sockfd, (struct sockaddr *)&rem_addr, sizeof(rem_addr)) < 0)
		{
			perror("Conectando stream socket");
			exit(1);
		}

		pthread_t threadId;
		pthread_create(&threadId, NULL, handleOutboundConnection, &rem_sockfd);
	}

	pthread_t receiveConnectionsThreadId;
	pthread_create(&receiveConnectionsThreadId, NULL, monitorIncomingConnections, NULL);

	pthread_t terminalThreadId;
	pthread_create(&terminalThreadId, NULL, monitorTerminalForCommands, NULL);

	pthread_join(receiveConnectionsThreadId, NULL);
	pthread_join(terminalThreadId, NULL);
}