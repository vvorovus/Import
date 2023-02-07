import os
import maillib
import email

# temp files in memory are faster than disk
try:
    # and the C routines even faster
    from cStringIO import StringIO
except ImportError:
    from StringIO import StringIO

# make mail exception availible in this namespace
MailError = maillib.MailError    

# set up a configuration dictionary with the recommended options
class Rule:
    pass

config_dict = {
    'sendmail_from': 'Your Name <your@address.here>',
    'sendmail_smtphost': 'smtp.your_own_domain.com',
    'sendmail_smtpport': 25, # you can normally leave this out, this is just for illustration
    'sendmail_user_agent':     'Catchy Slogan Here',
     
    'getmail_method': 'pop',    # or 'pop'
    'getmail_user': 'suvorov@mpaes.ru',
    'getmail_passwd': 'rjdfhysqgfhjkm',
    'getmail_server': '192.168.1.1', 
    'getmail_server_port': 110, # standard imap port, 110 is POP port

    #'debug_obj': my_filehandle, # totally optional, see below
    'debug_level': 0 # 0 is default, minimal verbosity, also optional
    }


my_mail = maillib.maillib(config_dict)     # you now have a maillib object

try:
    my_mail.getmail_login()
    Delete = 1 # delete the message after you fetch it
    print my_mail.get_num_of_msgs()
    for i in range(0,  my_mail.get_num_of_msgs()):
        raw_message = my_mail.fetch_first_msg(Delete)
        try:
            (header_dict, message_body, attachments_list) = my_mail.decode_email(raw_message)
        except MailError, msg:
            print 'There was a problem decoding the email.'
            print msg
        
        #print 'From: ', header_dict.get('From', 'unknown')
        #print 'To: ', header_dict.get('To', 'unknown')
        #print 'Subject: ', header_dict.get('Subject', 'unknown')
        print i
        try:
            if attachments_list:         
                for (attachment_description, mime_type, item_filename, attachment) in attachments_list:
                    (root, ext) = os.path.splitext(item_filename)
                    if ext.lower() in (".zip", ".xml", ".rar"): 
                        file = open("../" + item_filename, "wb")
                        file.write(attachment)
                        file.close() # probably unneccesary
        except ValueError:
            raise MailError("The attachments list you provided was not formed correctly; please read this functions comments.")
        
    my_mail.do_cleanup()  # logs off mail server!
except MailError, msg:
    print 'There was a problem retrieving the email.'
    print msg

### The raw unprocessed message is now in raw_message
 