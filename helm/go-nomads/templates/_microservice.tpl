{{/*
Generic microservice template
Usage: {{ include "go-nomads.microservice" (dict "root" . "name" "user-service" "config" .Values.userService "component" "backend") }}
*/}}
{{- define "go-nomads.microservice" -}}
{{- if .config.enabled }}
apiVersion: apps/v1
kind: Deployment
metadata:
  name: {{ .name }}
  namespace: {{ .root.Values.global.namespace }}
  labels:
    {{- include "go-nomads.componentLabels" (dict "root" .root "name" .name "component" .component) | nindent 4 }}
spec:
  replicas: {{ .config.replicaCount }}
  selector:
    matchLabels:
      {{- include "go-nomads.componentSelectorLabels" (dict "name" .name) | nindent 6 }}
  template:
    metadata:
      labels:
        {{- include "go-nomads.componentSelectorLabels" (dict "name" .name) | nindent 8 }}
      annotations:
        prometheus.io/scrape: "true"
        prometheus.io/port: "{{ .config.service.targetPort }}"
        prometheus.io/path: "/metrics"
    spec:
      {{- include "go-nomads.imagePullSecrets" .root | nindent 6 }}
      containers:
        - name: {{ .name }}
          image: {{ include "go-nomads.image" (dict "root" .root "image" .config.image) }}
          imagePullPolicy: {{ .root.Values.global.imagePullPolicy }}
          ports:
            - containerPort: {{ .config.service.targetPort }}
              name: http
          env:
            - name: ASPNETCORE_ENVIRONMENT
              valueFrom:
                configMapKeyRef:
                  name: go-nomads-config
                  key: ASPNETCORE_ENVIRONMENT
            - name: ASPNETCORE_URLS
              value: "http://+:{{ .config.service.targetPort }}"
            - name: ConnectionStrings__DefaultConnection
              valueFrom:
                secretKeyRef:
                  name: go-nomads-secrets
                  key: DATABASE_CONNECTION_STRING
            - name: ConnectionStrings__Redis
              valueFrom:
                secretKeyRef:
                  name: go-nomads-secrets
                  key: REDIS_CONNECTION_STRING
            - name: Supabase__Url
              valueFrom:
                secretKeyRef:
                  name: go-nomads-secrets
                  key: SUPABASE_URL
            - name: Supabase__Key
              valueFrom:
                secretKeyRef:
                  name: go-nomads-secrets
                  key: SUPABASE_KEY
            {{- if .extraEnv }}
            {{- toYaml .extraEnv | nindent 12 }}
            {{- end }}
          resources:
            {{- toYaml .config.resources | nindent 12 }}
          readinessProbe:
            httpGet:
              path: /health
              port: {{ .config.service.targetPort }}
            initialDelaySeconds: 10
            periodSeconds: 5
          livenessProbe:
            httpGet:
              path: /health
              port: {{ .config.service.targetPort }}
            initialDelaySeconds: 30
            periodSeconds: 15
---
apiVersion: v1
kind: Service
metadata:
  name: {{ .serviceName | default .name }}
  namespace: {{ .root.Values.global.namespace }}
  labels:
    {{- include "go-nomads.componentLabels" (dict "root" .root "name" .name "component" .component) | nindent 4 }}
spec:
  type: {{ .config.service.type }}
  ports:
    - port: {{ .config.service.port }}
      targetPort: {{ .config.service.targetPort }}
      name: http
  selector:
    {{- include "go-nomads.componentSelectorLabels" (dict "name" .name) | nindent 4 }}
{{- if .config.autoscaling.enabled }}
---
apiVersion: autoscaling/v2
kind: HorizontalPodAutoscaler
metadata:
  name: {{ .name }}-hpa
  namespace: {{ .root.Values.global.namespace }}
spec:
  scaleTargetRef:
    apiVersion: apps/v1
    kind: Deployment
    name: {{ .name }}
  minReplicas: {{ .config.autoscaling.minReplicas }}
  maxReplicas: {{ .config.autoscaling.maxReplicas }}
  metrics:
    - type: Resource
      resource:
        name: cpu
        target:
          type: Utilization
          averageUtilization: {{ .config.autoscaling.targetCPUUtilizationPercentage }}
{{- end }}
{{- end }}
{{- end }}
