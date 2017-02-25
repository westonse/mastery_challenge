#include <stdio.h>
#include <stdlib.h>
#include <unistd.h>
#include <string.h>
#include <sys/types.h>
#include <sys/socket.h>
#include <netinet/in.h>
#include <netdb.h> 
#include <time.h>
#include <stdlib.h>

int main(int argc, char *argv[])
{
	char randomletter;
	int r;
	int count;
	char alphabet[27] = {'A','B','C','D','E','F','G','H','I','J','K','L','M','N','O','P','Q','R','S','T','U','V','W','X','Y','Z',' '}; 
	int keylength = atoi(argv[1]);
	//printf("****%d",keylength);
	srand(time(NULL));
	for(count = 0; count<keylength; count++ ){ 
		r = rand() % 27;
		randomletter = alphabet[r];
		printf("%c",randomletter);
		fflush(stdout);
	}
		printf("\n");
		fflush(stdout);
	return 0;
}
