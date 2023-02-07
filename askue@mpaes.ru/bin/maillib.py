#!/usr/bin/env python

"""
 Python maillib -- higher-level interface to smtplib, poplib, imaplib,
 and the various MIME modules.  by Preston Landers -- pibble@yahoo.com

 Version 1.2.  First public release, September 1st 2001

 This is a general mail processing module designed to be simple but
 effective, not exhaustively complete in every aspect of mail arcana.
 Handles sending (smtp) recieving (POP and IMAP) and both decoding and
 encoding MIME messages/attachments (including multipart) using your
 choice of encodings.  (Currently only base64 is supported for
 outgoing attachments.)

 Provided courtesy of Journyx, Inc.
 Free time management on the web!

 http://freetimesheet.com
 http://journyx.com
 
 The license is the same as the current official Python license.

 written by Preston Landers <planders@hushmail.com> or <pibble@yahoo.com>
 first internal version January 2001

 Journyx, Inc. assumes no responsibility for this program. NO WARRANTY
 WRITTEN OR IMPLIED.  Do not use for controlling nuclear reactors,
 space stations, or dialysis machines for puppies and kittens. Blah
 blah blah blah...

 TODO:
 
   -- is hamfisted linefeed conversion going to cause problems?
   -- support for reading mbox and other mailbox formats
   -- handle other MIME types than mixed/multipart (such as signed)
   -- test suite

                       << INSTRUCTIONS >>
               
 Most problems raise MailError with a string explaining what's wrong.

 Sending mail is easy (one method call after instantiating) but
 recieving mail is slightly more complicated as you have to log in and
 out.  Just read these instructions below, in addition to the comments
 (.__doc__ strings) for each method.

  <<  1. SETUP >>
  
 import maillib

 # make mail exception availible in this namespace
 MailError = maillib.MailError    

 # set up a configuration dictionary with the recommended options

 config_dict = {

     'sendmail_from': 'Your Name <your@address.here>',
     'sendmail_smtphost': 'smtp.your_own_domain.com',
     'sendmail_smtpport': 25 # you can normally leave this out, this is just for illustration
     'sendmail_user_agent':     'Catchy Slogan Here',
     
     'getmail_method': 'imap',    # or 'pop'
     'getmail_user': 'journyx',
     'getmail_passwd': 'journyx',
     'getmail_server': 'imap-server.your_own_domain.com', 
     'getmail_server_port': 143, # standard imap port, 110 is POP port

     'debug_obj': my_filehandle, # totally optional, see below
     'debug_level': 1 # 0 is default, minimal verbosity, also optional
     }

 my_mail = maillib.maillib(config_dict)     # you now have a maillib object

  << 2. SEND MAIL >>

 mailto_list = ['destination@address.com']
 # you can also just give a plain string for 1 email address
 # multiple to: addresses must use list
 
 subject = 'Hello, maillib!'
 mail_body = 'Hello, world.'
 
 attachment_list = [] # no attachments, empty list or leave off

 try:
    my_mail.send_mail(mailto_list, subject, mail_body, attachment_list)
 except MailError, msg:
    print 'There was a mail sending problem'
    print msg
    raise

 print 'Mail sent ok.'

 # you can also validate email address(es) w/o instantiating the
 # maillib object

 import maillib
 invalid_list = maillib.validate_email_address(email_addr_list)

 any members of the returned invalid_list were not valid internet
 email addresses according to a simple regular expression.    If
 invalid_list is None they are all ok.

  << 3. SENDING ATTACHMENTS >>

  See 'sending mail' above, only you add attachment tuples to the
  attachment_list. See the send_mail() doc string for the format
  you are expected to use. 

  ie,

  description_1 = 'Picture of Tux the Penguin'
  filename_1 = 'tux.jpg'
  mimetype_1 = None # let the module guess from the filename, only works with common types
  item_1 = open_file_handle.read()
  attachment_1 = (description_1, mimetype_1, filename_1, item_1)

  attachment_list.append(attachment_1)

  # ... and so on for more attachments.

  << 4. RECEIVE EMAIL >>

 try:
    my_mail.getmail_login()
    Delete = 1 # delete the message after you fetch it
    raw_message = my_mail.fetch_first_msg(Delete)
    my_mail.do_cleanup()  # logs off mail server!
 except MailError, msg:
    print 'There was a problem retrieving the email.'
    print msg 

  ### The raw unprocessed message is now in raw_message

  << 5. DECODING MESSAGES >>

 try:
   (header_dict, message_body, attachments_list) = my_mail.decode_email(raw_message)
 except MailError, msg:
    print 'There was a problem decoding the email.'
    print msg

  print 'From: ', header_dict.get('From', 'unknown')
  print 'To: ', header_dict.get('To', 'unknown')
  print
  print message_body

  The attachment list is in the same format that you use to send
  attachments (a list of tuples, each tuple having 4 elements.)     The
  list can be empty, of course, if there were no attachments.  Some
  MUAs might include the message body as a MIME part and forget to
  mark it as 'inline,' in this case the message body might in the
  attachments list, and the message_body string would be empty.
  
  << 6. CLEANUP! >>

  just remember to call do_cleanup() on your maillib object to close
  the connection to the incoming mail server, as show in example 3.

  << 7. DEBUGGING >>

  There are two variables in the config_dict that control debugging /
  verbosity (such as printing extended errors from the mail server.)
  By default, 'debug_level' is 0, which means minimal verbosity.
  Actual errors will still get reported and exceptions raised.
  Currently, anything other than 0 will switch on 'maximum verbosity.'

  By default, the debugging info is written to stderr.    But you can
  put a 'debug_obj' in the config dictionary to make it write the
  debugging info elsewhere.     It can be a file handle or any object
  that has a write() method.

  << KNOWN BUGS / LIMITATIONS >>

 Should be obvious: sending mail requires having an smtp server
 somewhere that will talk to you. ;-) retrieving mail likewise
 requires a POP or IMAP server (be sure to say which in your
 config_dict -- it defaults to IMAP) and the appropriate
 login/password (which will be sent over the network as plaintext!)
 This module does not understand Unix concepts like /usr/bin/sendmail
 or procmail or local mail spools or MX records.

 Whoever recieves email sent by this module should be able to decode
 basic MIME messages, as the message body is itself a MIME part.  Most
 mail readers written after roughly the rise of technological
 civilization can without trouble.    However, if you are sending mail
 to barbarians, you can force non-MIME messages (which also disallows
 attachments.)

 Support for IMAP folders is relatively limited.  By default, all
 operations use the default 'INBOX' folder.     You can set self.Folder =
 'foo' to use another folder. There is currently no way to list all
 availible folders, for example.  Of course, POP doesn't have a
 concept of folders.

 Attachments in outgoing mail always uses base64 encoding.    Most MUAs
 can support this but a few ancient crufty ones cannot (and would
 probably have trouble with multipart MIME messages anyway.) Those
 programs should be banned anyway.

 """

__version__ = "1.2"
__copyright__ = "Copyright (C) 2001 Journyx, Inc. [Preston Landers <pibble@yahoo.com>]"

# standard Python stuff
import sys, exceptions, traceback, string, time, os, re, types, mimetypes

# temp files in memory are faster than disk
try:
    # and the C routines even faster
    from cStringIO import StringIO
except ImportError:
    from StringIO import StringIO

# MIME related imports
import MimeWriter
import mimetools
import multifile
import base64

# mail related imports
import smtplib

import socket # to catch socket exceptions directly

# are these modules availible?
M_POP = M_IMAP = 0

try:
    import poplib
    M_POP=1
except:
    sys.stderr.write("Support for POP3 not availible.\n")

try:
    import imaplib
    M_IMAP=1
except:
    sys.stderr.write("Support for IMAP4 not availible.\n")

# force mail sending capabilities to be availible
if not M_POP and not M_IMAP:
    sys.stderr.write("You don't have the required poplib / imaplib modules to use maillib!\n")
    raise SystemExit(1)

class MailError(exceptions.Exception):
    """Generic mail error exception.  Users of this module might want
    to import MailError into their namespace."""
    pass

class maillib:

    """See the module level __doc__ string for more information"""

    default_param_dict = {
        
        'sendmail_from': "maillib user <your@address.here>",
        'sendmail_encoding': 'base64',
        'sendmail_smtphost': "localhost",
        'sendmail_smtpport': 25,
        'sendmail_user_agent':    "Python maillib module (%s)" % (__version__),
        'sendmail_generic_attachment_description': "No Description Given",

        'getmail_method': "imap",  # or "pop"
        'getmail_user': "username",
        'getmail_passwd': "password",
        'getmail_server': "imap",
        'getmail_server_port': 143,
        'getmail_default_folder': "INBOX", # ignored for POP3
        #'getmail_server': "pop",
        #'getmail_server_port': 110,

        'debug_level': 0,
        'debug_obj': sys.stderr,
        }

    def __init__(self, param_dict):

        """Pass it a dictionary of parameters that will override the
        defaults.  You are recommended to set at least sendmail_from,
        sendmail_smtphost, getmail_user, getmail_passwd, and
        getmail_server.

        If Debug is set, verbosity will be increased (stuff is written
        to the debugobj.)"""

        # add all the new options to the base options (overwriting duplicates)
        self.params = self.default_param_dict
        for new_param_key in param_dict.keys():               
            self.params[new_param_key] = param_dict[new_param_key]

        self.debug = self.params['debug_level'] # if this flag is set, VERBOSITY
        self.debugobj = self.params['debug_obj'] # must have a write() method

        self.cleanup = None # set this flag if cleanup remains to be done
        self.is_logged_in = None # not logged in to getmail server

        self.Folder = self.params['getmail_default_folder']

        # check some key stuff
        method = self.params['getmail_method']
        self.is_imap = self.is_pop = 0
        if method == "pop":
            self.is_pop = 1
            if not M_POP:
                raise MailError("POP support is not availible (can't find poplib)")
        elif method == "imap":
            self.is_imap = 1
            if not M_IMAP:
                raise MailError("IMAP support is not availible (can't find imaplib)")
        else:
            raise MailError("Unsupported mail server type %s" % (method))
        

    ###
    ### MAIL SENDING
    ###

    def send_mail(self, mailto_list, subject, mail_body_text, attachments_list = None):

        """Actually sends off an email. Parameters are:

        mailto_list -- a list of destination email addresses (or a
        string for 1 address)

        subject -- the subject string

        mail_body_text -- the string that forms the main text of the
        message.  Can also be a filehandle that will be read() to get
        the message body.

        attachments_list -- a LIST OF TUPLES, one for each
        attachment.     Each tuple consists of 4 items:

          o description string of the object; if None is given a
          generic one will be used.

          o mimetype string.  You'll usually want 'text/html' for HTML
          docs.     If a mimetype is not given, it will be guessed from
          the filenames extension (if a file is given, otherwise a
          default generic mimetype is used.)

          o suggested filename string for the attachment; if you give
          it None here it will displayed in-line.  As a matter of good
          habit you should not use a full path ('/path/to/file.txt')
          but rather just the 'unqualified' name (ie, 'file.txt').

          o Finally, the attachment itself.     Normally a string (which
          can be text or binary data), but any object can be used
          which supports the __str__() or read() methods (they will be
          tried in that order.)     So you can pass an open filehandle.

        If attachments_list is None or an empty list, no attachments
        will be included.  The message body will still be a MIME-part. 

        Returns 0 for success, and a string with an error message if
        there was a problem."""

        ### sanity checks
        if not mailto_list:
            raise MailError("You need to supply at least one destination address!")

        ### Compose the email
        message_file = StringIO()

        MW = MimeWriter.MimeWriter(message_file)

        MW.addheader("From", self.params['sendmail_from'])

        if type(mailto_list) == types.StringType:
            mailto = mailto_list
        else:
            # create single string from all addresses
            mailto = ""
            for mail_addr in mailto_list:
                mailto = mailto + mail_addr + " , "
            mailto = mailto[:-3] # trim final comma and spaces

        MW.addheader("To", mailto)

        MW.addheader("Subject", subject)
        MW.addheader("MIME-Version", "1.0")
        MW.addheader("X-Mailer", self.params['sendmail_user_agent'])

        body = MW.startmultipartbody ("mixed")

        part = MW.nextpart()

        ### write the 'message body' here
        part.addheader("Content-Transfer-Encoding", "7bit")
        part.addheader("Content-Description", "message body text")
        part.addheader("Content-Disposition", "inline")
        item = part.startbody("text/plain; charset=us-ascii")

        if type(mail_body_text) == types.FileType:
            mail_body_text = mail_body_text.read()    
        item.write(mail_body_text)

        ### encode and attach the attachments
        try:
            if attachments_list:         
                for (attachment_description, mime_type, item_filename, attachment) in attachments_list:
                    # check for a read() method first
                    if hasattr(attachment, "read") and \
                       type(getattr(attachment, "read")) == types.MethodType:
                        attachment_fd = StringIO(attachment.read())
                    else:
                        # otherwise assume it's a string or can be stringified
                        attachment_fd = StringIO(str(attachment))

                    if not attachment_description:
                        attachment_description = self.params['sendmail_generic_attachment_description']

                    part = MW.nextpart()

                    encoding_type = self.params['sendmail_encoding']

                    part.addheader("Content-Transfer-Encoding", encoding_type)
                    part.addheader("Content-Description", attachment_description)

                    # if mime_type is not given but item_filename is, guess the mimetype
                    if mime_type == None and item_filename:
                        mime_type, item_encoding = mimetypes.guess_type(item_filename)
                        # if we STILL don't know use the generic
                        if mime_type == None:
                            mime_type == "mixed/octet-stream"

                    if item_filename:
                        part.addheader("Content-Disposition", "attachment; filename=\"%s\"" % item_filename)
                    else:
                        part.addheader("Content-Disposition", "inline")

                    # force HTML into mixed/octet-stream to keep certain
                    # MUAs from displaying it inline regardless of Content-Disposition
                    # XXX actually we may not need this... commenting it out for now.
                    #if mime_type == "text/html":
                    #     mime_type =  "mixed/octet-stream"

                    item = part.startbody(mime_type)

                    if encoding_type == "base64":
                        base64.encode(attachment_fd, item)
                    elif encoding_type == "quopri":
                        import quopri
                        # quotetabs = 1 # do quote tab characters
                        quotetabs = 0 # do not quote tab characters
                        quopri.encode(attachment_fd, item, quotetabs)
                    else:
                        raise MailError("Unsupported attachment encoding <%s>" % (encoding_type))

                    attachment_fd.close() # probably unneccesary
        except ValueError:
            raise MailError("The attachments list you provided was not formed correctly; please read this functions comments.")
        except:
            exc_name, exc_val, exc_tb = sys.exc_info()
            tb_lines = ""
            if self.debug:
                for line in traceback.format_exception(exc_name, exc_val, exc_tb):
                    self.debugobj.write("maillib: " + line)
            for line in traceback.format_tb(exc_tb):
                tb_lines = tb_lines + "\n" + line
            raise MailError("Exception forming attachments: %s :: %s %s" % (exc_name, exc_val, tb_lines))

        ### clean up
        MW.lastpart()
        smtphost = self.params['sendmail_smtphost']
        ### Send the email
        try:
            server = smtplib.SMTP(self.params['sendmail_smtphost'], self.params['sendmail_smtpport'])
            server.sendmail(self.params['sendmail_from'], mailto_list, message_file.getvalue())

        ### catch low-level socket errors like host not found
        except socket.error, Error:
            if type(Error) == type((1,)):
                errno, msg = Error
                raise MailError("Socket error: [%s] %s" % (errno, msg))
            else:
                raise MailError("Socket error: %s" % (Error))

        ### try to catch actual SMTP errors

        except smtplib.SMTPRecipientsRefused, msg:

            raise MailError("All recipients were refused by the SMTP server (%s). Message was not sent. Server said:\n%s" % (smtphost, msg))

        except smtplib.SMTPSenderRefused, msg:
        
            raise MailError("The SMTP server (%s) didn't accept the From: address (%s). Message was not sent. Server said:\n%s" % (smtphost, self.params['sendmail_from'], msg))

        except smtplib.SMTPDataError, msg:

            raise MailError("The SMTP server (%s) replied with an unexpected error code (other than a refusal of a recipient). Server said:\n%s" % (smtphost, msg))

        ### catch all other strange mail-sending errors
        except:
            exc_name, exc_val, exc_tb = sys.exc_info()

            # this exception not present in Python 1.5.1 so we must special-check it
            if str(exc_name) == 'smtplib.SMTPHeloError':
                raise MailError("The SMTP server (%s) didn't reply properly to the helo greeting.  Message was not sent. Server said:\n%s""" % (smtphost, exc_val))

            error_str = '\n'
            if self.debug:
                for line in traceback.format_exception(exc_name, exc_val, exc_tb):
                    self.debugobj.write("maillib: " + line)
                    error_str = error_str + line + "\n"
            raise MailError("Mail Error %s :: %s%s" % (exc_name, exc_val, error_str))
            
        ### if we got to this point, we're golden

        message_file.close() # makes it go bye-bye!
        server.quit() # clean up the SMTP object

        return 0 # success

    def get_params_dict(self, form_encoded_string):

        """Given a string that represents the encoding of an entire
        www form, return a dict mapping param to decoded value."""
        
        import urllib

        ret_dict = {}

        items = string.split(form_encoded_string, "&")

        for item in items:
            key, value = string.split(item, "=")
            ret_dict[urllib.unquote_plus(key)] = urllib.unquote_plus(value)

        return ret_dict

    ###
    ### MIME/Attachment PROCESSING
    ###

    def convert_linebreaks_to_unix(self, text):

        """Given a text string, return the string with all
        MSDOS/Windows/Mac style line breaks converted to Unix
        style.    TODO: is this kosher?"""

        text = string.replace(text,"\015\012", "\012") # MSDOS/Win
        text = string.replace(text,"\015", "\012") # Mac

        return text
           

    def decode_email(self, raw_message):

        """This method is used to decode a raw message -- it will
        return a special tuple representing the decoded mail
        (described below.  Pass it a string that represents the entire
        raw message.

        You will get back a tuple in this form:

        (header_dict, message_body_string, [Attachments List if neccesary])

        header_dict is a dictionary mapping header items to
        values. (Such as 'From':'foobar <foo@bar.com>').
        message_body_string is the plain text of the message (decoded
        if neccesary.)    If there were attachments in the mail, you
        will be given an attachments list (otherwise you will get an
        empty list here.)

        The Attachments list is futher composed of tuples, one for
        each attachment, with four items, the same as in the
        dispatch_email function.

        (description, mimetype, filename, decoded_attachment)

        If an item such as description or filename is not provided in
        the mail, None will be used.
        
        You will not get back the original raw message string so save
        that if you need to.

        """

        # heresy? 
        raw_message = self.convert_linebreaks_to_unix(raw_message)

        header_dict, message_body = self.decode_headers(raw_message)
        attachment_list = []

        message_file = StringIO(raw_message)
        message = mimetools.Message(message_file)

        plain_email = None # assume its a MIME msg until we know otherwise

        ### test for plain text or MIME

        mime_version = message.getheader("MIME-Version")
        if not mime_version:        
            plain_email = 1

        content_type = message.getheader("Content-Type")
        if not content_type:
            plain_email = 1

        if plain_email:
            timestamp = time.strftime("%m_%d_%Y_%H:%M:%S", time.localtime(time.time()))
            filename = "non_mime_message_%s.txt" % (timestamp)

            if self.debug:
                self.debugobj.write("Does not appear to be a MIME message; writing full message to <%s>\n" % filename)

            return (header_dict, message_body, attachment_list)

        ### ok we seem to have a MIME message but is it multipart or not?
        message_body = "" # in a MIME message the body will be an encoded part so we reset this
        item_list = []
        boundary = message.getparam("boundary")

        if string.find(content_type, "multipart/mixed") > -1:
            ### handle multipart MIME
            if self.debug:
                self.debugobj.write("Handling multipart MIME message.\n")

            mf = multifile.MultiFile(message.fp, 0)
            mf.push(boundary)

            while not mf.last:
                mf.next()

                item = mf.read()

                item_list.append(item)          
        else:
            ### assume its just one item, try to decode the whole msg
            if self.debug:
                self.debugobj.write("Handling single MIME part. %s\n" % (content_type))
            item_list.append(raw_message)

        # decode each item
        for item in item_list:              

            is_inline = self.is_part_inline(item)
            decoded_item = self.mime_decode(item)

            # if it's meant to be displayed inline
            if is_inline:
                # include it in the body text
                message_body = message_body + decoded_item + "\n"
            else:
                # otherwise add it to the attachments list
                description = self.get_part_description(item)
                filename = self.get_part_filename(item)
                mimetype = self.get_part_mimetype(item)
                
                attachment = (description, mimetype, filename, decoded_item)
                attachment_list.append(attachment)

        return (header_dict, message_body, attachment_list)

    def decode_headers(self, raw_message):

        """Returns a 2-tuple: (a dictionary based on the mail headers,
        and the remaining (raw) body of the mail.).     Headers are
        defined to be the first block of text followed by two
        linefeeds."""

        return_dict = {}

        converted_item = self.convert_linebreaks_to_unix(raw_message)

        # now split into headers and body
        parts = string.split(converted_item, "\n\n", 1)           
        headers = parts[0]
        body = parts[1]

        # header_re = re.compile('(\w+?)[:]\s(.*?)(\n\w(.*?))|($)', re.M)
        
        header_lines = []

        current_line = ''
        
        for line in string.split(headers, "\n"):
            if line[0] in string.whitespace:
                current_line = current_line + line[1:]
            else:
                if current_line:
                    header_lines.append(current_line)
                current_line = line
            
        for line in header_lines:
            try:
                key, val = string.split(line, ":", 1)
            except ValueError, msg:
                self.debugobj.write("maillib: mail header line broken: %s\n" % (repr(line)))
                continue
            val = string.strip(val)
            key = string.strip(key)
            return_dict[key] = val

        return (return_dict, body)

    def is_part_inline(self, item):

        """Returns 1 if the part is marked to display inline, else
        returns 0"""

        if string.find(item, "Content-Disposition: inline") > -1:
            return 1
        return 0

    def get_part_mimetype(self, item):

        """Returns the mimetype if known."""

        mimetype = None
        match = re.search("Content-Type: (.*?)\\;?$", item, re.M | re.I)
        if match:
            mimetype = match.group(1)

        return mimetype

    def get_part_description(self, item):

        """Returns the item description if availible, else returns None."""

        # get a description if possible
        description = None
        match = re.search("Content-Description: (.*?)$", item, re.M | re.I)
        if match:
            description = match.group(1)

        return description

    def get_part_filename(self, item):

        """scan the item for a suitable filename or make one up."""

        filename = None

        # check for specified filename
        match = re.search("Content-Disposition.*?\\s+?filename=\"?(.*?[^\"\\s])\"?$", item, re.M | re.I)
        if match:
            filename = match.group(1)
            return filename

        # ok they didn't specify a filename

        # check for specified filename
        match = re.search("Content-Type.*?\\s+?name=\"?(.*?[^\"\\s])\"?$", item, re.M | re.I)
        if match:
            filename = match.group(1)
            return filename

        # get a description if possible
        description = None
        match = re.search("Content-Description: (.*?)$", item, re.M | re.I)
        if match:
            description = match.group(1)

        # if its plaintext generate a unique name
        if string.find(item, "Content-Type: text/plain") > -1:
            # if its inline text assume it is the message body
            if string.find(item, "Content-Disposition: inline") > -1:
                return "message.body.%s.txt" % str(int(time.time()))

            # use description if provided
            if description:
                return description + ".txt"

            # just make up something at this point
            return "attached." + str(int(time.time())) + ".txt"

        # its not plain text, and they didn't supply a filename
        # TODO: use system file(1) command if availible
        # use the description if possible
        filename = "unknown.file." + str(int(time.time()))
        if description:
            return    filename + "." + description
        else:
            return filename

    def mime_decode(self, item):

        """Attempt to decode a MIME item.  I only understand these
        encodings: base64, quoted-printable, uuencode, 7bit.  Returns the
        unencoded item as a string."""

        encoding = "7bit"  # just assume this unless we can tell otherwise

        # trim the fat (split thing on first \n\n) UGLY!
        # this also tells us to set encoding to 7bit (if there's no fat to trim)
        try:
            # first convert all MS/DOS and Mac linebreaks to Eunooks
            #converted_item = string.replace(item, "\015\012", "\012")
            #converted_item = string.replace(converted_item, "\015", "\012")

            # now split into headers and body
            parts = string.split(item, "\n\n", 1)         
            headers = parts[0]      
            item = parts[1]
        except IndexError:
            # must be just the item, no headers
            pass

        # figure out the encoding
        match = re.search("Content-Transfer-Encoding: (.*?)$", headers, re.M | re.I)
        if match:
            encoding = match.group(1)
        else:
            # self.debugobj.write("I could not determine the encoding of this part, defaulting to <%s> :-(\n" % (encoding))
            pass

        # self.debugobj.write("Decoding MIME part with encoding %s\n" % (encoding))
        encoding = encoding.lower()
        if encoding == "quoted-printable" or encoding == "quopri":
            import quopri
            infile = StringIO(item)
            outfile = StringIO()
            quopri.decode(infile, outfile)
            return outfile.getvalue()
        elif encoding == "base64":
            return base64.decodestring(item)
        elif encoding == "uuencode":
            import uu
            infile = StringIO(item)
            outfile = StringIO()
            uu.decode(infile, outfile)
            return outfile.getvalue()
        elif encoding == "8bit":
            return item
        elif encoding == "7bit":
            return item
        else:
            self.debugobj.write("Warning! I don't understand encoding <%s>\n" % (encoding))
            return item

    def get_replyto(self, header_dict):

        """Given a message header dictionary, return a list of
        addresses appropriate to reply to the message.    Checks a
        reply-to header field first, then checks From or Sender."""

        my_headers = {}

        for key in header_dict.keys():
            my_headers[string.lower(key)] = string.lower(header_dict[key])
        
        replyto = my_headers.get("reply-to", None)

        if not replyto:
            replyto = my_headers.get("from", None)
            if not replyto:
                replyto = my_headers.get("sender", None)
                if not replyto:
                    return [''] # XXX TODO what to do here?

        # split up multiple addresses
        reply_list = string.split(replyto, ",")
        
        return reply_list
            
    ###
    ### MAIL RETRIEVAL
    ###

    def getmail_login(self):

        """Call this method to initialize a connection to the mail
        (retrieval) server.     This is neccesary before you can check #
        of messages or download any.  Be sure to call do_cleanup() to
        log out of the server.

        Raises MailError if there is a problem, otherwise returns
        None."""
            
        # initialize mail server
        mserver = self.params['getmail_server']
        mserver_port = self.params['getmail_server_port']
        uname = self.params['getmail_user']
        passwd = self.params['getmail_passwd']
        
        if self.is_imap:
            self.server = imaplib.IMAP4(mserver, mserver_port)
        elif self.is_pop:
            self.server = poplib.POP3(mserver, mserver_port)
        
        # log into server
        try:
            if self.is_imap:
                self.server.login(uname, passwd)
            else:
                self.server.user(uname)
                self.server.pass_(passwd)
                if self.debug:
                    welcome = self.server.getwelcome()
                    self.debugobj.write("getmail server says: %s\n" % welcome)
                
        except imaplib.IMAP4.error, msg:
            raise MailError("IMAP Error: " + str(msg))
        except poplib.error_proto, msg:
            raise MailError("POP Error: " + str(msg))

        self.cleanup = 1  # we have stuff to clean up
        self.is_logged_in = 1 # we are logged in

        return 1
            
    def get_num_of_msgs(self):

        """returns the integer whole number of messages on the server.
        Returns None if you are not logged in or otherwise there are
        no msgs availible."""

        if not self.is_logged_in:
            return None

        msg_count = 0

        if self.is_imap:
            msg_status, msg_list = self.server.select(self.Folder) # select INBOX
            msg_count = int(msg_list[0])
            # self.debugobj.write("msg_count %s\n" % (repr(msg_count)))
        else:
            msg_count, mailbox_size = self.server.stat()
            msg_count = int(msg_count)

        return msg_count

    def get_first_message_id(self):

        """returns the integer ID of the first message in the mailbox.
        If no messages availibe, or you are not logged in, return
        None."""

        if not self.is_logged_in:
            return None

        msg_id = None

        if self.is_imap:
            self.server.select(self.Folder)
            typ, data = self.server.search(None, 'ALL')

            if data == ['']:                
                return None # no messages!
                
            msg_id = string.split(data[0])[0]

            self.server.close()

        else:  # handle POP3
            result_list = self.server.list()
            status = result_list[0]
            msg_list = result_list[1]

            # make sure we got an OK for the message list request
            if string.find(status, "OK") == -1:
                self.debugobj.write("Mail Server Error Output: [%s] %s\n" % (status, repr(msg_list)))
                raise MailError("Couldn't get message list from POP server.")
            
            if msg_list == [''] or msg_list == []:
                return None # no messages!

            # find the ID of the very first mesg in the list
            msg_id = string.split(msg_list[0])[0]

        return int(msg_id)


    def fetch_message(self, message_id):

        """given the integer message id (you must determine the
        appropriate one yourself, perhaps with other methods in this
        module) return the string containing the raw message
        Raises MailError if there was a problem with the server or you
        call this method without being logged in."""

        if not self.is_logged_in:
            raise MailError("You are not logged in to retrieve mail.")

        message = ""

        if self.is_imap:
            self.server.select(self.Folder)
            typ, data = self.server.fetch(message_id, '(RFC822)')

            message = data[0][1]

            self.server.close()

        else: # POP
            response = self.server.retr(message_id)

            # make sure we understand the response
            if len(response) == 3:
                status, msg_lines, octets = response
            else:
                raise MailError("Unknown response format: %s" % (repr(response)))

            # make sure we got an OK for the message fetch
            if string.find(status, "OK") == -1:
                self.debugobj.write("Output: [%s] %s\n" % (status, repr(response)))
                raise MailError("Couldn't get the message from the POP server.")

            if type(msg_lines) == type([]):
                message = string.join(msg_lines, "\n")
            elif type(msg_lines) == type(''):
                message = msg_lines
            
        return message

    def fetch_first_msg(self, Delete = None):

        """returns the next raw message in the mailbox (next == first
        availible) If Delete is set, the message will be deleted (and
        returned as a string)

        If no mail is present or you are not logged in, raises
        MailError."""

        if not self.is_logged_in:
            raise MailError("You are not logged in to retrieve mail.")

        first_msg_num = self.get_first_message_id()

        if not first_msg_num:
            raise MailError("There are no messages availible to download.")

        message = self.fetch_message(first_msg_num)

        if Delete:
            if not self.delete_msg(first_msg_num):
                raise MailError("There was a problem deleting Message %s from the mail server!" % (first_msg_num))
  
        return message

    def delete_msg(self, msg_id):

        """delete the given message ID on the server. returns the 1 if
        message was deleted sucessfully, else None."""

        if not self.is_logged_in:
            raise MailError("You are not logged in to retrieve mail.")

        ret_val = None

        if self.is_imap:
            self.server.select(self.Folder)

            flag_list = "\\Deleted"
            msg_set = msg_id
            command = "+FLAGS"

            result = self.server.store(msg_set, command, flag_list)

            if self.debug:
                self.debugobj.write("delete result: < %s >\n" % (repr(result)))

            if result[0] == "OK":                 
                ret_val = 1
            else:
                ret_val = None

            # Warning! This expunges the messages!
            self.server.close()

        else:
            result = self.server.dele(msg_id)
            if self.debug:
                self.debugobj.write("Delete result: %s\n" % (repr(result)))
            ret_val = 1

        return ret_val

    def do_cleanup(self):

        """do any cleanup before logging off the mail server.  __del__
        tries to call this if neccesary, but you are advised to call
        it yourself so exceptions will be caught."""
        
        if self.cleanup:
            try:
                if self.is_imap:
                    self.server.logout()
                else:
                    self.server.quit()
                self.is_logged_in = 0 # clear logged-in flag
            except:
                exc_name, exc_val, exc_tb = sys.exc_info()
                for line in traceback.format_exception(exc_name, exc_val, exc_tb):
                    self.debugobj.write("EXC: " + line + "\n")
        self.cleanup = 0  # clear cleanup flag
        return 1             
            
    def __del__(self):

        """cleanup methods when this object goes away."""

        if self.cleanup or self.is_logged_in:
            self.do_cleanup()

    def add_msg_to_mailbox(self, message):

        """Appends the message to the given mailbox (if not supplied,
        the default is used for POP and INBOX for IMAP. Returns 1 if
        succesfull else None."""

        if self.is_imap:
            flags = ""
            date_time = int(time.time())
            
            result = self.server.append(self.Folder, flags, date_time, message)

            if result[0] == "OK":
                return 1
            return None
                            
        else:
            raise MailError("POP servers not supported for add_msg_to_mailbox().")

        return None

# just compile this regex once and store it
# the regexp roughly means a valid email address must have:
#
#  * a username (any non-whitespace characters) followed by @ 
#
#  * a domain/subdomain set made of letters, numbers, or dots 
#
#  * a final dot and a two or three letter Top Level Domain
#  
valid_addr_re = re.compile("^(\S+?)[@](\w[\w.-]+?)+?[.](\w{2,3})$")

def validate_email_address(email_addr):

    """Returns None if the email address(es) (you can give 1 string or
    a list of strings) are considered 'valid' for general internet use
    (host@domain.tld roughly).    If one or more addresses was invalid,
    returns them in a list."""

    if type(email_addr) == type(""):
        email_addr = [email_addr]
    elif not (type(email_addr) == type([])):
        raise MailError("Pass this method a string or list of strings. Got: %s" % (repr(email_addr)))

    rejected_addrs = []

    for email in email_addr:
        email = str(email)
        match = valid_addr_re.search(email)
        if not match:
            rejected_addrs.append(email)

    if not len(rejected_addrs):
        return None

    return rejected_addrs


if __name__ == "__main__":
    print "maillib.py ", __version__,  __copyright__
    print "This is intended to be used as a module, not a standalone program."
    raise SystemExit(1)
