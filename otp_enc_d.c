///
//  otp_enc_d.c
//  program 4
//  CS 344
//  Winter 2016
//  Created by Seth Weston on 3/5/16.
//  

/*LIBRARY INCLUDES*/ 
#include <stdlib.h>
#include <stdio.h>
#include <sys/types.h>
#include <sys/socket.h>
#include <netinet/in.h>
#include <string.h>
#include <unistd.h>
#include <sys/wait.h>

/*max tranmission length*/
#define MAXLINE 1000 
/*maximum number of client connections*/
#define QUE 5 

// Function main(argc argv)
// Main implements concurrent server funcionality that performs encryption of text
// sent by client(otp_enc) and sends it back properly encrypted with the one time pad
// Returns 0 upon success otherwise returns int > 1
int main (int argc, char **argv)
{
 /*integers used for ascci values of chars and socket file descriptor*/
 int listenfd, connfd, n, cur_mes_char, cur_key_char, enc_char;
 /*pid of forked process in concurrent server*/
 pid_t childpid;
 /*client variables and buffer*/
 socklen_t clilen;
 char buf[MAXLINE];
 struct sockaddr_in cliaddr, servaddr;
 /*count variables*/
 int count, count2;
 /*num tranmission variables used to keep track of what to do with recieved text*/
 int num_transmissions,num_key_transmissions, num_mes_transmissions;
 /*temp used for appending key*/
 char* temp;
 /*full key string*/
 char* key; 
 /*current start position of key string*/
 int cur_key_start = 0;
 
 /*If sockfd<0 there was an error in the creation of the socket*/
 if ((listenfd = socket (AF_INET, SOCK_STREAM, 0)) <0) {
  perror("Problem in creating the socket");
  exit(2);
 }


 /*socket addressing*/
 servaddr.sin_family = AF_INET;
 servaddr.sin_addr.s_addr = htonl(INADDR_ANY);
 servaddr.sin_port = htons(atoi(argv[1]));

 /*bind socket*/
 bind (listenfd, (struct sockaddr *) &servaddr, sizeof(servaddr));

 /*listen to the socket by creating a queue*/
 listen (listenfd, QUE);

 for ( ; ; ) {
  
  /*accept a connection*/
  clilen = sizeof(cliaddr);
  connfd = accept (listenfd, (struct sockaddr *) &cliaddr, &clilen);

  /*create child process for each connection*/
  if ( (childpid = fork ()) == 0 ) {

    /*close listen socket and set count to 0*/
    close (listenfd);
    count = 0;
    /*loop until everything has been recioeved*/
    while ( (n = recv(connfd, buf, MAXLINE,0)) > 0)  {
     /*if it is first transmission then get the number of tranmissions sent from client*/
     if(count == 0){
	num_transmissions = atoi(strtok(buf," "));
	num_key_transmissions = atoi(strtok(NULL," "));
	num_mes_transmissions = num_transmissions - num_key_transmissions; 
     }
     /*if count<numkey tranmissions then append the key string*/
     else if(count > 0 && count <= num_key_transmissions){
	/*Append key if not first key transmission else copy to key string*/
	if(count > 1){
			temp = (char*) malloc(1 + strlen(key));
			strcpy(temp,key);
			key = (char*) malloc(1 + strlen(buf) + strlen(temp));
	 		strcpy(key,temp);
			strcat(key,buf);
			//printf("%s\n%s\n",key,temp);
		}
		else{
			key = (char*) malloc(1 + strlen(buf)); 
			strcpy(key,buf);
		}
     }
     /*else encrypt the text that was recieved*/
     else{
	/*Encrypt each character in buf*/
     	for(count2 = 0; count2<strlen(buf); count2++){
	 	/*if character in buf is a space set it to a value of 27 because it is the 27th character in our alphabet used for the program*/    	
		if(buf[count2] == 32)
			cur_mes_char = 27;
		/*else set it to its asci value - 64 in order to represent A = 1 and Z = 26*/
		else
			cur_mes_char = buf[count2] - 64;//A=1 "space" = 27
		/*if character in key is a space set it to a value of 27 because it is the 27th character in our alphabet used for the program*/
		if(key[count2 + cur_key_start] == 32)
			cur_key_char = 27;
		/*else set it to its asci value - 64 in order to represent A = 1 and Z = 26*/
		else
			cur_key_char = key[count2 + cur_key_start] - 64;
		/*add two characters together*/
		enc_char = cur_mes_char + cur_key_char;
		/*if mod % 27 > 0 then subtract 27*/
		if(enc_char > 27 && enc_char != 54){
			enc_char = enc_char - 27;
			buf[count2] = enc_char + 64;
		}
		/*check if both characters are a space (because 54 % 27 = 0)*/
		else if(enc_char == 27 || enc_char == 54){
			buf[count2] = 32;
		}
		/*else reset character to proper asci value*/
		else{
			buf[count2] = enc_char + 64;
		}		
	}
        /*move starting position of key*/
	cur_key_start += MAXLINE;
    }
     /*send whatever is in buf to client and increment count*/
     send(connfd, buf, n, 0);
     bzero(buf,MAXLINE);
     count++;
    }
    
    if (n < 0)
      printf("%s\n", "Read error");
    exit(0);
  }
  if(childpid == -1){
	printf("error forking");
	exit(1);	
  } /*close server socket*/
   close(connfd);
  
 }
}
