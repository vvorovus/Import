import os
import poplib
import email
import mimetypes
import string
import io


config_dict = {
    'getmail_user': 'askue@mpaes.ru',
    'getmail_passwd': 'askue132',
    'getmail_server': '192.168.1.1', 
    'getmail_server_port': 110, # standard imap port, 110 is POP port

    #'debug_obj': my_filehandle, # totally optional, see below
    'debug_level': 0 # 0 is default, minimal verbosity, also optional
    }


my_mail = poplib.POP3(config_dict['getmail_server'], config_dict['getmail_server_port'])
my_mail.user(config_dict['getmail_user'])
my_mail.pass_(config_dict['getmail_passwd'])
print(my_mail.getwelcome())
print('-----------------------')

my_mail.list()
(numMsgs, totalSize) = my_mail.stat()
print('Messages:', numMsgs)
print('Size:', totalSize)
print('-----------------------')
for i in range(1, numMsgs + 1):
    s = my_mail.retr(i)
    (header, msg, octets) = my_mail.retr(i)
    #print("Message %d:" % i)
    s = ''
    for line in msg: 
        line = line.decode()
        s = s + line + '\n'

    msg = email.message_from_string(s)
    #with open('spam.txt', 'w') as file:
    #    file.write(s)

    counter = 1

    for part in msg.walk():
        filename = part.get_filename()
        if not filename:
            ext = mimetypes.guess_extension(part.get_content_type())
            if not ext:
                # Use a generic bag-of-bits extension
                ext = '.bin'
            filename = 'part-%03d%s' % (counter, ext)

        (root, ext) = os.path.splitext(filename)

        if ext.lower() in (".zip", ".xml", ".rar"): 
            file = open("../" + filename, "wb")
            file.write(part.get_payload(decode=True))
            file.close() # probably unneccesary
            print(filename)

        counter += 1

    print('-----------------------')
    my_mail.dele(i)
my_mail.quit()

### The raw unprocessed message is now in raw_message
