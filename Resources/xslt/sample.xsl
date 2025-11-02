<?xml version="1.0" encoding="UTF-8"?>
<xsl:stylesheet version="1.0"
 xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
  <xsl:output method="html" indent="yes"/>
  <xsl:template match="/">
    <html>
      <head><title>Student Parliament Events</title></head>
      <body>
        <h1>Student Parliament Events</h1>
        <table border="1" cellpadding="4" cellspacing="0">
          <tr>
            <th>P.I.P.</th><th>Faculty</th><th>Department</th><th>Specialty</th><th>Window</th><th>Type</th>
          </tr>
          <xsl:for-each select="students/student">
            <tr>
              <td><xsl:value-of select="fullname"/></td>
              <td><xsl:value-of select="faculty"/></td>
              <td><xsl:value-of select="department"/></td>
              <td><xsl:value-of select="specialty"/></td>
              <td><xsl:value-of select="eventWindow"/></td>
              <td><xsl:value-of select="parliamentType"/></td>
            </tr>
          </xsl:for-each>
        </table>
      </body>
    </html>
  </xsl:template>
</xsl:stylesheet>