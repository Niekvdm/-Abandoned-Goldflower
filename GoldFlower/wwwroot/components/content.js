const AWAITINGUSERINPUT = 0;
const INSTALLING = 1;
const ABORTED = 2;
const FINISHED = 3;
const CANCELLED = 4;
const IDLE = 5;
const FAILED = 6;

import TopBar from './navigation/topbar.js';
import ContentHeader from './header.js';
import Files from './files.js';

export default {
	components: {
		TopBar,
		ContentHeader,
		Files
	},
	name: 'main-content',
	data() {
		return {
			path: window.path,
			files: [],
			selected: [],
			error: null,
			warning: null,
			events: [],
			installer: {
				status: IDLE,
				progress: 0,
				currentFile: null,
				files: [],
				events: null,
				processorType: 0
			},
			AWAITINGUSERINPUT: AWAITINGUSERINPUT,
			INSTALLING: INSTALLING,
			ABORTED: ABORTED,
			FINISHED: FINISHED,
			CANCELLED: CANCELLED,
			IDLE: IDLE,
			FAILED: FAILED,
			processors: {
				NONE: 0,
				TINFOIL: 1,
				GOLDLEAF: 2
			}
		};
	},
	template: `
		<div>
			<top-bar :events="sortedEvents"></top-bar>
			<content-header :type="installer.processorType" @change="installer.processorType = $event"></content-header>
			
			<div class="container-fluid mt--6" v-if="installer.processorType === processors.NONE">
				<div class="row">
					<div class="col-md-6 mb-6">
						<div class="card card-profile">
							<img src="/images/switch.jpg" alt="Image placeholder" class="card-img-top">
							<div class="card-img-overlay d-flex align-items-center">
								<div class="w-100">
									<h5 class="h2 card-title text-white text-shadow-black mb-2 text-center">Goldleaf</h5>
								</div>
							</div>
							<div class="row justify-content-center">
								<div class="col-lg-3 order-lg-2">
									<div class="card-profile-image">
										<a href="#" @click="setProcessorType(processors.GOLDLEAF)">
											<img src="/images/goldleaf.png" class="rounded-circle">
										</a>
									</div>
								</div>
							</div>
						</div>
					</div>

					<div class="col-md-6">
						<div class="card card-profile">
							<img src="/images/switch.jpg" alt="Image placeholder" class="card-img-top">						
							<div class="card-img-overlay d-flex align-items-center">
								<div class="w-100">
									<h5 class="h2 card-title text-white text-shadow-black mb-2 text-center">Tinfoil</h5>
								</div>
							</div>
							<div class="row justify-content-center">
								<div class="col-lg-3 order-lg-2">
									<div class="card-profile-image">
										<a href="#" @click="setProcessorType(processors.TINFOIL)">
											<img src="/images/empty.png" class="rounded-circle">
										</a>
									</div>
								</div>
							</div>
						</div>
					</div>
				</div>
			</div>

			<div class="container-fluid mt--6" v-else-if="installer.processorType !== processors.NONE">
				<div class="row">
					<div class="col">

						<div class="card bg-default">
							<!-- Card header -->
							<div class="card-header bg-default shadow">
								<h3 class="mb-0 text-white">Enter NSP directory</h3>
							</div>

							<!-- Card body -->
							<div class="card-body">							
								<div class="form-group">
									<div class="input-group input-group-merge">
										<div class="input-group-prepend">
											<span class="input-group-text"><i class="fa fa-file"></i></span>
										</div>
										<input class="form-control" placeholder="C:\\your\\directory" type="text" v-model="path">
									</div>
								</div>

								<div class="form-group mb-0">
									<button class="btn btn-secondary" @click="onSelectDirectoryClicked">Search directory</button>
								</div>
							</div>
						</div>
							
					</div>
				</div>

				<div class="actions clearfix" style="margin-bottom: 10px;">
					<div class="pull-right">
						<button class="btn btn-neutral" v-if="!isInstalling" @click="onInstallCicked" :disabled="!selected.length">
							<span class="fa fa-upload"></span> Start installation
						</button>

						<button class="btn btn-danger" v-if="isInstalling && installer.status !== FINISHED" @click="onAbortClicked">
							<span class="fa fa-close"></span> Abort installation
						</button>

						<button class="btn btn-success pull-right" @click="onCompleteClicked" v-else-if="isInstalling && installer.status === FINISHED">
							<span class="fa fa-check"></span> Complete installation
						</button>
					</div>
				</div>
				
				<files :files="files" :installer="installer" @change="onSelectedFilesChanged"></files>
	
			</div>
		</div>
	`,
	mounted() {
		if (this.path != null) {
			this.onSelectDirectoryClicked();
		}

		this.getInstallProgress();
	},
	methods: {
		onSelectDirectoryClicked() {
			this.files = [];
			this.error = null;
			this.warning = null;

			axios
				.post('installer/select-directory', { path: this.path })
				.then(response => {
					if (response.data.result) {
						this.files = response.data.result;
					} else if (response.data.error) {
						this.error = response.data.error;
					}
				})
				.catch(error => {
					this.error = error;
					console.error(error);
				});
		},

		translateProcessorType(type) {
			switch (type) {
				case 1:
					return 'Tinfoil';
				case 2:
					return 'Goldleaf';
				default:
					return '-';
			}
		},

		onInstallCicked() {
			this.events = [];

			axios
				.post(`installer/install/${this.installer.processorType}`, this.selected)
				.then(response => {
					if (response.data) {
						this.setInstaller(response.data);
						this.getInstallProgress();
					} else if (response.data.error) {
						this.error = response.data.error;
					}
				})
				.catch(error => {
					this.error = error;
					console.error(error);
				});
		},

		getInstallProgress() {
			axios
				.get('installer/progress')
				.then(response => {
					if (response.data) {
						this.setInstaller(response.data);

						if (this.installer.status === INSTALLING || this.installer.status === AWAITINGUSERINPUT) {
							setTimeout(() => {
								this.getInstallProgress();
							}, 1000);
						}
					} else if (response.data.error) {
						this.error = response.data.error;
					}
				})
				.catch(error => {
					console.error(error);

					setTimeout(() => {
						this.getInstallProgress();
					}, 2500);
				});
		},

		setInstaller(data) {
			this.installer = data;

			for (let event of data.events) {
				if (!this.events.find(x => x.type === event.type && x.dateTime === event.dateTime)) {
					if (this.events.length >= 50) {
						this.events.splice(0, 1);
					}

					this.events.push(event);
				}
			}

			// Deselect installed
			if (data.files) {
				for (let file of data.files) {
					if (file.state === FINISHED) {
						let index = this.selected.findIndex(x => x.name === file.name);

						if (index > -1) {
							this.selected.splice(index, 1);
						}
					}
				}
			}

			this.warning = null;
			this.error = null;

			if (data.status === ABORTED) {
				this.error = 'Installation was aborted by the user';
			} else if (data.status === CANCELLED) {
				this.error = 'An error occured during installation';
			} else if (data.status === AWAITINGUSERINPUT) {
				switch (data.processorType) {
					case 0:
						this.warning = '<p><strong>Awaiting user input</strong></p>Select <strong><i>Title management</i></strong> followed by <strong><i>USB installation</i></strong> in the Tinfoil app on the Switch';
						break;
					case 1:
						this.warning = '<p><strong>Awaiting user input</strong></p>Select <strong><i>USB installation</i></strong> in the Goldleaf app on the Switch<br />Select your prefered options in the Goldleaf app to start the installation';
						break;
				}
			}
		},

		setProcessorType(type) {
			this.events = [];

			axios
				.post(`installer/processorType/${type}`)
				.then(response => {
					if (response.data) {
						this.installer.processorType = type;
						this.selected = [];
						this.$forceUpdate();
					} else if (response.data.error) {
						this.error = response.data.error;
					}
				})
				.catch(error => {
					this.error = error;
					console.error(error);
				});
		},

		onAbortClicked() {
			if (confirm('Are you sure you want to abort the installation?')) {
				axios
					.post('installer/abort')
					.then(response => {
						if (response.data) {
							this.setInstaller(response.data);
						} else if (response.data.error) {
							this.error = response.data.error;
						}
					})
					.catch(error => {
						this.error = error;
						console.error(error);
					});
			}
		},

		onCompleteClicked() {
			this.events = [];

			axios
				.post('installer/complete')
				.then(response => {
					if (response.data) {
						this.selected = [];
						this.setInstaller(response.data);
						this.$forceUpdate();
					} else if (response.data.error) {
						this.error = response.data.error;
					}
				})
				.catch(error => {
					this.error = error;
					console.error(error);
				});
		},

		onSelectedFilesChanged(selectedFiles) {
			this.selected = selectedFiles;
		}
	},
	computed: {
		sortedEvents() {
			return this.events.sort((a, b) => (a.dateTime < b.dateTime ? 1 : b.dateTime < a.dateTime ? -1 : 0));
		},

		isInstalling() {
			return this.installer.status === INSTALLING || this.installer.status == AWAITINGUSERINPUT;
		}
	}
};
