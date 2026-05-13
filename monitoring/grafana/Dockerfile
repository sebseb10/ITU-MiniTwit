FROM grafana/grafana:12.1

COPY dashboards /etc/grafana/provisioning/dashboards/
COPY datasources /etc/grafana/provisioning/datasources/

USER grafana

EXPOSE 3000